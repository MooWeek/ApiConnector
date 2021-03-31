using System;
using System.ComponentModel;
using Newtonsoft.Json;
using oda.Attributes;
using System.Xml;

namespace oda
{
    public sealed class Init : MainInit
    {
        private const string IdAPIConnectorClass = "1D5AC9A3883EF47";

        private Session _session = null;
        private string Path = null;
        private Object SourceObject = null;


        /// <summary>
        /// Возвращает объект класса APIConnector
        /// </summary>
        private void SetSourceObj()
        {
            if (SourceClass == null)
            {
                ErrorProvider.ShowError("АпиКоннектор: Ошибка при получении SourceObject. Отсутствует SourceClass", "Ошибка при получении свойства");
                return;
            }
            Class APIConnectorClass = Domain.GetClass(IdAPIConnectorClass);
            // Поиск объекта в APIConnector'е по FullId вызывающего объекта
            xmlDocument xDoc = new xmlDocument(
                APIConnectorClass.XQuery("PACK/OBJECT[@IsActive='True']/Target[contains('" +
                Path +
                "',@link)]/..")
            );
            SourceObject = APIConnectorClass.FindObject(xDoc.SelectElement("OBJECT"));
        }

        /// <summary>
        /// Возвращает объект сессии
        /// </summary>
        private Session Session
        {
            get
            {
                if (_session == null)
                {
                    // Создаём объект сессии
                    _session = new Session();
                }

                return _session;
            }
        }

        [IgnoreAbstract(true)]
        [RunContext(ItemType.Class | ItemType.Object)]
        [DisplayName("Отправка запроса по таймеру")]
        [MethodType(MethodType.ClassEvent)]
        public void SendRequestTimer()
        {
            AfterTimer();
        }

        [Active(false)]
        [Browsable(true)]
        [IgnoreAbstract(true)]
        [RunContext(ItemType.Class | ItemType.Object)]
        [ViewContext(ItemType.Class | ItemType.Object)]
        [DisplayName("Запуск запроса по таймеру")]
        [ViewMode(ViewModes.ServiceButton)]
        public void SendRequestTimerButton()
        {
            AfterTimer();
        }

        [IgnoreAbstract(true)]
        [RunContext(ItemType.Object)]
        [MethodType(MethodType.ObjectEvent)]
        [DisplayName("Отправить запрос после сохранения")]
        public void SendRequestSave()
        {           
            AfterSave(Object);
        }


        [Active(false)]
        [Browsable(true)]
        [IgnoreAbstract(true)]
        [RunContext(ItemType.Object)]
        [ViewContext(ItemType.Object)]
        [DisplayName("Отправить запрос")]
        [ViewMode(ViewModes.ServiceButton)]
        public void SendRequestButton()
        {
            AfterSave(Object);
        }

        /// <summary>
        /// Производит поиск методов ApiConnector`a которые работаю по событию "после сохранения объекта"(AfterSave)
        /// </summary>
        void AfterSave(Object obj)
        {
            try
            {
                Path = obj.Class.FullId;
                SetSourceObj();
                // Выбираем все методы, содержащие AfterSave
                if (SourceObject.Root.XQuery("//Params[contains(@EventType,'ByTimer')]") == null)
                {
                    Messages.showException(new Exception("Не найдено объекта класса ApiConnector для исполнения запроса от даного класса"));
                    return;
                }
                xmlDocument paramsDoc = new xmlDocument(
                    "<PARAMS>" +
                    SourceObject.Root.XQuery("//Params[contains(@EventType,'AfterSave')]") +
                    "</PARAMS>"
                );
                Connect(paramsDoc, obj);
            }
            catch (Exception ex)
            {
                Messages.showMessage("АпиКоннектор: Не удалось отправить запрос после сохранения,   " + ex.Message);
            }
        }

        /// <summary>
        /// Производит поиск методов ApiConnector`a которые работаю по таймеру(ByTimer)
        /// </summary>
        void AfterTimer()
        {
            try
            {
                Path = Class.FullId;
                SetSourceObj();
                // Выбираем все методы, содержащие ByTimer
                if (SourceObject.Root.XQuery("//Params[contains(@EventType,'ByTimer')]") == null)
                {
                    Messages.showException(new Exception("Не найдено объекта класса ApiConnector для исполнения запроса от даного класса"));
                    return;
                }
                xmlDocument paramsDoc = new xmlDocument(
                    "<PARAMS>" +
                    SourceObject.Root.XQuery("//Params[contains(@EventType,'ByTimer')]") +
                    "</PARAMS>"
                );

                Connect(paramsDoc, SourceObject);
            }
            catch (Exception ex)
            {
                Messages.showMessage("АпиКоннектор ,   Не удалось отправить запрос по таймеру,   " + ex.Message);
            }
        }

        /// <summary>
        /// Подключает нужный выд запроса, создает его екземпляр и отправляет запрос
        /// </summary>
        /// <param name="paramsDoc">xmlDocument с параметрами по которым будет выполнятся запрос</param>
        /// <param name="sourceObject">Объект от которого выполняется REST запрос</param>
        internal void Connect(xmlDocument paramsDoc, Object sourceObject)
        {
            sourceObject.XmlDoc = null;
            sourceObject.Recalc();
            // Обход найденных методов
            foreach (xmlElement sourceElement in paramsDoc.Root.ChildElements)
            {
                if (!String.IsNullOrEmpty(sourceElement.GetAttribute("XqueryForObjects")))
                {
                    Path = Class.FullId;
                    SetSourceObj();

                    MakeRequestsFromObjIndex(sourceElement);
                }
                else
                {
                    try
                    {
                        MakeSimpleRequests(sourceObject, sourceElement);
                    }
                    catch (System.IO.FileNotFoundException e)
                    {
                        Messages.showException(e);
                    }

                }
            }
        }

        /// <summary>
        /// Создает екземпляры Запросов от контекста каждого объекта в Индексе
        /// </summary>
        /// <param name="sourceElement">Метод с параметрами</param>
        void MakeRequestsFromObjIndex(xmlElement sourceElement)
        {
            string XqueryForObjects = sourceElement.GetAttribute("XqueryForObjects");

            var objcs = Class.XQuery(XqueryForObjects);

            xmlDocument objs = new xmlDocument(objcs);

            if (objs.Root == null)
                return;

            foreach (var i in objs.Root.ChildElements)
            {
                Object CurrentObj = GetCurrentTargetObject(i);

                UpdateSourceObject(CurrentObj);

                if (CurrentObj.XQuery(sourceElement.GetAttribute("NeedWork")) == "False")
                    continue;

                Request request = new Request(SourceObject, CurrentObj, sourceElement)
                {
                    TargetClass = Class
                };

                SetRequestData(CurrentObj, sourceElement, request);

                Session.ExecuteQuery(request);
            }
        }

        /// <summary>
        /// Обновление указаного объекта
        /// </summary>
        /// <param name="currentObj">Объект</param>
        private void UpdateSourceObject(Object currentObj)
        {
            using (new UpdateObject(SourceObject))
            {
                SourceObject.Root.SetAttribute("ObjDate", currentObj.XQuery(SourceObject.GetAttrValue("XquryObj")));
            }
            SourceObject.Save();
        }

        /// <summary>
        /// Нахождение целевого объета
        /// </summary>
        /// <param name="targObject">Узел с @oid целевого объекта</param>
        /// <returns></returns>
        private Object GetCurrentTargetObject(xmlNode targObject)
        {
            string oid = targObject.XQuery("string(@oid)");
            return Class.GetObject(oid);
        }

        /// <summary>
        /// Отправка одиночного запроса
        /// </summary>
        /// <param name="obj">Объект от которого отправляется запрос</param>
        /// <param name="sourceElement">xmlElement с входящими настройками запроса</param>
        void MakeSimpleRequests(Object obj, xmlElement sourceElement)
        {
            if (obj.XQuery(sourceElement.GetAttribute("NeedWork")) == "False")
                return;

            if (!string.IsNullOrEmpty(SourceObject.GetAttrValue("XquryObj")))
            {
                UpdateSourceObject(obj);
            }
            // Формируем запрос к сторонней БД

            Request request = new Request(SourceObject, obj, sourceElement)
            {
                TargetClass = Class
            };

            SetRequestData(obj, sourceElement, request);

            // Выполняем запрос к сторонней БД
            Session.ExecuteQuery(request);
        }

        /// <summary>
        /// Запись Данных в екземпляр запроса
        /// </summary>
        /// <param name="obj">Объект от которого отправляется запрос</param>
        /// <param name="sourceElement">xmlElement с входящими настройками запроса</param>
        /// <param name="request">екземпляр запроса</param>
        private void SetRequestData(Object obj, xmlElement sourceElement, Request request) 
        {
            string RequestFormat = sourceElement.GetAttribute("RequestFormat");
            string dataString = "";
            if (sourceElement.GetAttribute("MethodQuery") != "")
            {
                dataString = obj.XQuery(sourceElement.GetAttribute("MethodQuery"));
            }

            if (string.IsNullOrEmpty(RequestFormat) || RequestFormat == "XML" || RequestFormat == "Without conversion")
            {
                //Устанавливаем данные для POST-запросов
                request.SetData(dataString);
            }
            else
            {
                if (!string.IsNullOrEmpty(dataString))
                {
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(dataString);
                    dataString = JsonConvert.SerializeXmlNode(doc, Newtonsoft.Json.Formatting.None, true);
                    request.SetData(dataString.Replace(",null", ""));
                }
            }
        }
    }
}