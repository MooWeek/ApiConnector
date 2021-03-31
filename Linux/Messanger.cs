using System;
using System.Xml.Linq;
using Newtonsoft.Json;
using System.Timers;
using System.Net.Http;

namespace oda
{
    internal class Messanger
    {

        static Request _request = null;

        /// <summary>
        /// Проверка на корректность Веб-запроса, конвертация и отправка
        /// </summary>
        /// <param name="request">Запрос</param>
        /// <returns>Ответ от Запроса</returns>
        internal static bool SendRequest(Request request)
        {
            if (request.URL == null)
            {
                Messages.showMessage(request.TemplateObject + ",   " + request.URL + ",   Не создан URL");
                return false;
            }
            try
            {
                if (request.MethodType == "GET") {
                    return GetRequest(request);
                }

                return PostRequest(request);
            }
            catch (Exception ex)
            {
                Messages.showMessage("АпиКоннектор: Не удалось отправить запрос - " + ex.Message);
                return false;
            }            
        }

        /// <summary>
        /// Выполняет GET запрос к серверу и получает ответ
        /// </summary>
        /// <param name="request">Запрос на отправку</param>
        /// <returns>Возвращает ответ от сервера</returns>
        internal static bool GetRequest(Request request)
        {
            if (request == null)
            {
                Messages.showMessage("АпиКоннектор: Запрос для отправки пуст.");
                return false;
            }

            string responseBody;
            try
            {
                HttpResponseMessage httpResponse = request.HttpClient.GetAsync(request.URL).Result;
                httpResponse.EnsureSuccessStatusCode();
                responseBody = httpResponse.Content.ReadAsStringAsync().Result;
            }
            catch(HttpRequestException ex) 
            {
                Messages.showMessage("АпиКОннектор: Не удалось отправить запрос - " + ex.Message);
                return false;
            }

            return ResponseProcessing(request, responseBody);
        }

        /// <summary>
        /// Выполняет POST запрос к серверу и получает ответ
        /// </summary>
        /// <param name="request">Запрос на отправку</param>
        /// <returns>Возвращает ответ от сервера</returns>
        internal static bool PostRequest(Request request) 
        {
            if (request == null)
            {
                Messages.showMessage("АпиКоннектор: Запрос для отправки пуст.    SendRequest");
                return false;
            }
            string responseBody;
            try
            {
                HttpResponseMessage httpResponse = request.HttpClient.PostAsync(request.URL, request.DataContent).Result;
                httpResponse.EnsureSuccessStatusCode();
                responseBody = httpResponse.Content.ReadAsStringAsync().Result;
            }
            catch(HttpRequestException ex) 
            {
                Messages.showMessage("АпиКоннектор: Ошибка при отправке Post запроса - " + ex.Message);
                return false;
            }
            return ResponseProcessing(request, responseBody);
        }

        /// <summary>
        /// Производит обработку и конвертацию ответу ответа
        /// </summary>
        /// <param name="request">REST Запрос</param>
        /// <param name="responseBody">Тело ответа</param>
        /// <returns>Строка ответа REST запроса</returns>
        internal static bool ResponseProcessing(Request request, string responseBody)
        {
            xmlDocument xDoc = new xmlDocument(ConvertingResponce(request, responseBody));
            string requestData;
            if (request.DataContent == null)
                requestData = "";
            else
                requestData = request.DataContent.ToString();
            ApiLogger.Log(request.TemplateObject, request.TemplateElement, xDoc, request.URL, requestData);

            if (request.SourceXmlDocument != null)
            {
                WriteResponse(xDoc, request);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Производит запись ответа от сервера в вызывающий объект
        /// </summary>
        /// <param name="xDoc">Документ от которого будет сохранятся объект</param>
        /// <param name="request">Запрос</param>
        internal static void WriteResponse(xmlDocument xDoc, Request request) 
        {
            string XQuryOneObj = request.TemplateElement.GetAttribute("XquryXML").Replace("[#OBJXML#]", request.SourceXmlDocument.Root.XML);
            string NewXML = xDoc.XQuery(XQuryOneObj);
            request.SourceXmlDocument.ReplaceChild(new xmlDocument(NewXML).Root, request.SourceXmlDocument.Root);
            string AfterSaveXQ = request.TemplateElement.GetAttribute("AfterSaveXQ");
            if (!string.IsNullOrEmpty(AfterSaveXQ))
            {
                xDoc.XQuery(AfterSaveXQ.Replace("[#OBJXML#]", request.SourceXmlDocument.Root.XML));
            }
        }

        /// <summary>
        /// Конвертация ответа
        /// </summary>
        /// <param name="request">Запрос</param>
        /// <param name="responseBody">Тело ответа</param>
        /// <returns>Конвертированое тело ответа</returns>
        internal static string ConvertingResponce(Request request, string responseBody) 
        {
            string NeedElement = request.TemplateElement.GetAttribute("NeedBaseElement");
            if (NeedElement == "True")
            {
                responseBody = "{\"Root\":" + (string)responseBody + "}";
            }
            return ConvertTo(responseBody, request.TemplateElement.GetAttribute("AcceptedType"));
        }

        /// <summary>
        /// Конвертация в JSON
        /// </summary>
        /// <param name="responce">Ответ запроса</param>
        /// <param name="type">Тип в который нужно конвертировать</param>
        /// <returns>Возвращает конвертированый ответ</returns>
        static string ConvertTo(string responce, string type) 
        {
            string convertingResponce = responce;
            if (type == "JSON")
            {  
                // Пишем в лог URL запроса, данные запроса, ответ от сервера
                XNode node = JsonConvert.DeserializeXNode(responce, "Root");
                xmlDocument xDoc = new xmlDocument(node.ToString());
                convertingResponce = xDoc.XML;
            }
            return convertingResponce;
        }
    }
}