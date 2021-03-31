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
        private Object TemplateObject = null;


        /// <summary>
        /// Возвращает объект класса APIConnector
        /// </summary>
        private void SetSourceObj()
        {
            if (SourceClass == null)
            {
                ErrorProvider.ShowError("Ошибка при получении SourceObject. Отсутствует SourceClass", "Ошибка при получении свойства");
                return;
            }
            Class APIConnectorClass = Domain.GetClass(IdAPIConnectorClass);
            // Поиск объекта в APIConnector'е по FullId вызывающего объекта
            xmlDocument xDoc = new xmlDocument(
                APIConnectorClass.XQuery("PACK/OBJECT[@IsActive='True']/Target[contains('" +
                Path +
                "',@link)]/..")
            );
            TemplateObject = APIConnectorClass.FindObject(xDoc.SelectElement("OBJECT"));
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
            try
            {
                AfterTimer();
            }
            catch (Exception ex)
            {
                Messages.showMessage("АпиКоннектор: Не удалось отправить запрос по таймеру,   " + ex.Message);
            }
        }

        [IgnoreAbstract(true)]
        [RunContext(ItemType.Object)]
        [MethodType(MethodType.ObjectEvent)]
        [DisplayName("Отправить запрос после сохранения")]
        public void SendRequestSave()
        {
            try
            {
                if (!IsFileDirExist(Object))
                {
                    using (new UpdateObject(Object))
                    {
                        Object.Root.SetAttribute("FileDirExist"," True");
                    }
                    Object.Save();
                    return;
                }
                AfterSave(Object);
            }
            catch (Exception ex)
            {
                Messages.showMessage("АпиКоннектор: Не удалось отправить запрос после сохранения,   " + ex.Message);
            }
        }

        private bool IsFileDirExist(Object obj)
        {
            return obj.Root.GetBool("FileDirExist");
        }

        /// <summary>
        /// Производит поиск методов ApiConnector`a которые работаю по событию "после сохранения объекта"(AfterSave)
        /// </summary>
        private void AfterSave(Object obj)
        {
             Path = obj.Class.FullId;
             SetSourceObj();
             // Выбираем все методы, содержащие AfterSave
             if (TemplateObject.Root.XQuery("//Params[contains(@EventType,'ByTimer')]") == null)
             {
                 Messages.showException(new Exception("Не найдено объекта класса ApiConnector для исполнения запроса от даного класса"));
                 return;
             }
             xmlDocument paramsDoc = new xmlDocument(
                 "<PARAMS>" +
                 TemplateObject.Root.XQuery("//Params[contains(@EventType,'AfterSave')]") +
                 "</PARAMS>"
             );
             Connect(obj, paramsDoc);
        }

        /// <summary>
        /// Производит поиск методов ApiConnector`a которые работаю по таймеру(ByTimer)
        /// </summary>
        private void AfterTimer()
        {
            Path = Class.FullId;
            SetSourceObj();
            // Выбираем все методы, содержащие ByTimer
            if (TemplateObject.Root.XQuery("//Params[contains(@EventType,'ByTimer')]") == null)
            {
                Messages.showException(new Exception("Не найдено объекта класса ApiConnector для исполнения запроса от даного класса"));
                return;
            }
            xmlDocument paramsDoc = new xmlDocument(
                "<PARAMS>" +
                TemplateObject.Root.XQuery("//Params[contains(@EventType,'ByTimer')]") +
                "</PARAMS>"
            );

            Connect(TemplateObject, paramsDoc);
        }

        /// <summary>
        /// Подключает нужный выд запроса, создает его екземпляр и отправляет запрос
        /// </summary>
        /// <param name="paramsDoc">xmlDocument с параметрами по которым будет выполнятся запрос</param>
        /// <param name="sourceObject">Объект от которого выполняется REST запрос</param>
        internal void Connect(Object sourceObject, xmlDocument paramsDoc)
        {
            bool needSave = false;
            xmlDocument sourceXmlDocument = new xmlDocument(sourceObject.Root.XML);
            // Обход найденных объектов ApiConnector`a с параметрами  
            foreach (xmlElement templateElement in paramsDoc.Root.ChildElements)
            {
                if (!String.IsNullOrEmpty(templateElement.GetAttribute("XqueryForObjects")))
                {
                    Path = Class.FullId;
                    SetSourceObj();

                    MakeRequestsFromObjIndex(templateElement);
                }
                else
                {
                    try
                    {
                        needSave = needSave || MakeSimpleRequests(sourceObject, sourceXmlDocument, templateElement);
                    }
                    catch (System.IO.FileNotFoundException e)
                    {
                        Messages.showException(e);
                    }

                }
            }

            if (needSave)
                sourceObject.Save(sourceXmlDocument.Root.XML);
        }

        /// <summary>
        /// Создает екземпляры Запросов от контекста каждого объекта в Индексе
        /// </summary>
        /// <param name="templateElement">Метод с параметрами</param>
        private bool MakeRequestsFromObjIndex(xmlElement templateElement)
        {

            string XqueryForObjects = templateElement.GetAttribute("XqueryForObjects");

            var objcs = Class.XQuery(XqueryForObjects);

            xmlDocument objs = new xmlDocument(objcs);

            if (objs.Root == null)
                return false;

            foreach (var node in objs.Root.ChildElements)
            {
                Object sourceObject = GetSourceTargetObject(node);
                xmlDocument sourceXmlDocument = new xmlDocument(sourceObject.Root.XML);
                UpdateTemplateObject(sourceXmlDocument);

                if (sourceXmlDocument.Root.XQuery(templateElement.GetAttribute("NeedWork")) == "False")
                    continue;

                // Формируем запрос к сторонней БД
                if (CreateAndFillRequest(sourceObject, sourceXmlDocument, templateElement))
                {
                    sourceObject.Save(sourceXmlDocument.Root.XML);
                }
            }
            return true;
        }

        /// <summary>
        /// Нахождение целевого объета
        /// </summary>
        /// <param name="node">Узел с @oid целевого объекта</param>
        /// <returns></returns>
        private Object GetSourceTargetObject(xmlNode node)
        {
            string oid = node.XQuery("string(@oid)");
            return Class.GetObject(oid);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceObject">Объект от которого отправляется запрос</param>
        /// <param name="sourceXmlDocument">xmlDocument от которого отправляется запрос</param>
        /// <param name="templateElement">xmlElement с входящими настройками запроса</param>
        /// <returns></returns>
        private bool MakeSimpleRequests(Object sourceObject, xmlDocument sourceXmlDocument, xmlElement templateElement)
        {
            if (sourceXmlDocument.Root.XQuery(templateElement.GetAttribute("NeedWork")) == "False")
            {
                Messages.showMessage("АпиКоннектор: NeedWork = false. XQ - " + templateElement.GetAttribute("NeedWork"));
                return false;
            }
            Messages.showMessage("АпиКоннектор: Выполнение запроса NeedWork. XQ - " + templateElement.GetAttribute("NeedWork"));
            if (!string.IsNullOrEmpty(TemplateObject.GetAttrValue("XquryObj")))
            {
                UpdateTemplateObject(sourceXmlDocument);
            }
            // Формируем запрос к сторонней БД            
            return CreateAndFillRequest(sourceObject, sourceXmlDocument, templateElement);
        }

        /// <summary>
        /// Создание запроса и заполение его данными
        /// </summary>
        /// <param name="sourceObject">Объект от которого отправляется запрос</param>
        /// <param name="sourceXmlDocument">xmlDocument от которого отправляется запрос</param>
        /// <param name="templateElement">xmlElement с входящими настройками запроса</param>
        /// <returns></returns>
        private bool CreateAndFillRequest(Object sourceObject, xmlDocument sourceXmlDocument, xmlElement templateElement) 
        {
            Request request = new Request(sourceObject, TemplateObject, sourceXmlDocument, templateElement)
            {
                TargetClass = Class
            };
            SetRequestData(sourceXmlDocument, templateElement, request);
            // Выполняем запрос к сторонней БД

            return Session.ExecuteQuery(request);
        }

        /// <summary>
        /// Обновление Объекта
        /// </summary>
        /// <param name="sourceXmlDocument">Документ с даными дляобновления</param>
        private void UpdateTemplateObject(xmlDocument sourceXmlDocument) 
        {

            using (new UpdateObject(TemplateObject))
            {
                TemplateObject.Root.SetAttribute("ObjDate", sourceXmlDocument.Root.XQuery(TemplateObject.GetAttrValue("XquryObj")));
            }
            // Сохраняем изменения в статическом объекте
            TemplateObject.Save();
        }

        /// <summary>
        /// Установка данных запросов
        /// </summary>
        /// <param name="sourceXmlDocument">xmlDocument от которого отправляется запрос</param>
        /// <param name="templateElement">xmlElement с входящими настройками запроса</param>
        /// <param name="request">экземпляр запроса</param>
        private void SetRequestData(xmlDocument sourceXmlDocument, xmlElement templateElement, Request request) 
        {
            string RequestFormat = templateElement.GetAttribute("RequestFormat");
            string dataContent = "";
            if (templateElement.GetAttribute("MethodQuery") != "")
            {
                dataContent = sourceXmlDocument.Root.XQuery(templateElement.GetAttribute("MethodQuery"));
            }
            if (request.ContentType == "multipart/form-data")
            {
                //Устанавливаем данные для multipart/form-data-запросов
                request.DataContent = request.SetMultipartFormData();
            }

            if (string.IsNullOrEmpty(RequestFormat) || RequestFormat == "XML" || RequestFormat == "Without conversion")
            {
                //Устанавливаем данные для POST-запросов
                request.SetData(dataContent);
            }
            else
            {
                if (!string.IsNullOrEmpty(dataContent))
                {
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(dataContent);
                    dataContent = JsonConvert.SerializeXmlNode(doc, Newtonsoft.Json.Formatting.None, true);
                    request.SetData(dataContent.Replace(",null", ""));
                }
            }
        }
    }
}