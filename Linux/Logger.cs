using System;

namespace oda
{
    internal class ApiLogger
    {

        /// <summary>
        /// Запись Лога об успешном выполнении
        /// </summary>
        /// <param name="SourceObj">Объект ApiConnector`a c параметрами REST запроса</param>
        /// <param name="SourceElement">Елемент объекта ApiConnector`a c параметрами REST запроса</param>
        /// <param name="xDoc">xmlDocument ответа</param>
        /// <param name="RequestURL">URL Запроса</param>
        /// <param name="RequestData">Данные Запроса</param>
        internal static void Log(Object SourceObj, xmlElement SourceElement, xmlDocument xDoc, string RequestURL, string RequestData)
        {
            // Выполняем поиск объекта для ведения логов
            Class cls = SourceObj.Class.Domain.FindClass(SourceObj.Root.XQuery("string(LogClass/@link)"));
            string Xqury = SourceElement.GetAttribute("LogXqury");
            string LogXml = xDoc.XQuery(Xqury);
            if (cls != null)
            {
                if (!String.IsNullOrEmpty(LogXml))
                {
                    CreateLog(cls, RequestURL, RequestData, LogXml);
                }
                else
                {
                    CreateLog(cls, RequestURL, RequestData, xDoc);
                }
            }
        }

        /// <summary>
        /// Запись Лога об ошибке
        /// </summary>
        /// <param name="SourceObj">Объект ApiConnector`a c параметрами REST запроса</param>
        /// <param name="RequestURL">URL Запроса</param>
        /// <param name="Error">Описанте ошибки</param>
        internal static void SystemLog(Object SourceObj, string RequestURL, string Error)
        {
            // Выполняем поиск объекта для ведения логов
            Class cls = SourceObj.Class.Domain.GetClass(SourceObj.Root.XQuery("string(LogClass/@link)"));
            if (cls != null)
            {
                CreateLog(cls, RequestURL, Error);
            }
        }

        /// <summary>
        /// Создание лога без ответа
        /// </summary>
        /// <param name="cls">Класс в котором нужно создать лог</param>
        /// <param name="requestURL">URL Запроса</param>
        /// <param name="requestData">Данные Запроса</param>
        /// <param name="logXml">Xml объекта который нужно создать</param>
        internal static void CreateLog(Class cls, string requestURL, string requestData, string logXml) 
        {
            // Создём новый объект в логах
            Object logObject = cls.CreateObject(logXml);
            logObject.Root.SetAttribute("RequestURL", requestURL);
            logObject.Root.SetAttribute("RequestData", requestData);
            // Сохраняем объект логов
            logObject.Save();
        }

        /// <summary>
        /// Создание лога с ответом
        /// </summary>
        /// <param name="cls">Класс в котором нужно создать лог</param>
        /// <param name="requestURL">URL Запроса</param>
        /// <param name="requestData">Данные Запроса</param>
        /// <param name="xDoc">Документ ответа запроса</param>
        internal static void CreateLog(Class cls, string requestURL, string requestData, xmlDocument xDoc)
        {
            // Создём новый объект в логах
            Object logObject = cls.CreateObject();
            logObject.Root.SetAttribute("Response", xDoc.XML);
            logObject.Root.SetAttribute("RequestURL", requestURL);
            logObject.Root.SetAttribute("RequestData", requestData);
            // Сохраняем объект логов
            logObject.Save();
        }

        /// <summary>
        /// Создание лога с ошибкой
        /// </summary>
        /// <param name="cls">Класс в котором нужно создать лог</param>
        /// <param name="requestURL">URL Запроса</param>
        /// <param name="error">Описание ошибки</param>
        internal static void CreateLog(Class cls, string requestURL, string error)
        {
            // Создём новый объект в логах
            Object logObject = cls.CreateObject();
            logObject.Root.SetAttribute("Request", requestURL);
            logObject.Root.SetAttribute("Error", error);
            logObject.Root.SetAttribute("Clients", "SystemLOG");
            // Сохраняем объект логов
            logObject.Save();
        }
    }
}