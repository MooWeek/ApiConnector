using System;
using System.Net;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using System.Xml.Linq;

namespace oda
{
    internal class Messanger
    {
        /// <summary>
        /// Проверка на корректность Веб-запроса, конвертация и отправка
        /// </summary>
        /// <param name="request">Запрос</param>
        /// <returns>Ответ от Запроса</returns>
        internal static string SendRequest(Request request)
        {
            if (request.WebRequest == null)
            {
                ApiLogger.SystemLog(request.SourceObject, request.URL, "Не создан объект WebRequest");
                return "";
            }
            try
            {
                // Проверяем есть ли данные на отправку. Если данных нет, отправляем запрос без передачи данных
                if (request == null || !request.HasData)
                    return GetResponse(request);

                // Конвертим символы в байты

                byte[] sentData = request.Data;
                // Получаем длину массива байт
                request.WebRequest.ContentLength = sentData.Length;

                // Создаём поток на запись данных
                using (Stream sendStream = request.GetRequestStream())
                {
                    // Записываем данные
                    sendStream.Write(sentData, 0, sentData.Length);
                    // Закрываем поток на запись
                    sendStream.Close();
                }
                // Отправляем запрос на получение данных
                return GetResponse(request);
            }
            catch (Exception ex)
            {
                ErrorProvider.ShowError(ex.Message, "Post");
                return "";
            }            
        }

        /// <summary>
        /// Выполняет запрос к серверу и получает ответ
        /// </summary>
        /// <param name="request">Запрос на отправку</param>
        /// <param name="sender">Вызывающий метод</param>
        /// <returns>Возвращает ответ от сервера</returns>
        internal static string GetResponse(Request request)
        {
            if (request == null)
            {
                ErrorProvider.ShowError("АпиКоннектор: Запрос для отправки пуст.", "SendRequest");
                return String.Empty;
            }

            string responseBody = String.Empty;
            // Получаем ответ от сервера по запросу
            HttpWebResponse response = request.GetResponse();

            // Получаем поток для чтения ответа от сервера
            using (Stream respStream = response.GetResponseStream())
            {
                // Создаём ридер потока
                using (StreamReader stream = new StreamReader(respStream, Encoding.UTF8))
                {
                    // Читаем ответ до конца
                    responseBody = stream.ReadToEnd();
                    // Закрываем ридер
                    stream.Close();
                }
                // Закрываем поток на чтение
                respStream.Close();
            }
            return ResponseProcessing(request, responseBody);
        }

        /// <summary>
        /// Производит обработку и конвертацию ответу ответа
        /// </summary>
        /// <param name="request">REST Запрос</param>
        /// <param name="responseBody">Тело ответа</param>
        /// <returns>Строка ответа REST запроса</returns>
        internal static string ResponseProcessing(Request request, string responseBody)
        {
            xmlDocument xDoc = new xmlDocument(ConvertingResponce(request, responseBody));
            ApiLogger.Log(request.SourceObject, request.SourceElement, xDoc, request.URL, request.DataString);

            if (request.TargetObject != null)
            {
                WriteResponce(xDoc, request);
            }
            return responseBody;
        }

        /// <summary>
        /// Производит запись ответа от сервера в вызывающий объект
        /// </summary>
        /// <param name="xDoc">Документ от которого будет сохранятся объект</param>
        /// <param name="request">Запрос</param>
        internal static void WriteResponce(xmlDocument xDoc, Request request) 
        {
            string XQuryOneObj = request.SourceElement.GetAttribute("XquryXML").Replace("[#OBJXML#]", request.TargetObject.XML);
            string NewXML = xDoc.XQuery(XQuryOneObj);
            request.TargetObject.Save(NewXML);
            string AfterSaveXQ = request.SourceElement.GetAttribute("AfterSaveXQ");
            if (!string.IsNullOrEmpty(AfterSaveXQ))
            {
                xDoc.XQuery(AfterSaveXQ.Replace("[#OBJXML#]", request.TargetObject.XML));
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
            string NeedElement = request.SourceElement.GetAttribute("NeedBaseElement");
            if (NeedElement == "True")
            {
                responseBody = "{\"Root\":" + responseBody + "}";
            }
            string convertType = request.SourceElement.GetAttribute("AcceptedType");
            string convertingBody = ConvertTo(responseBody, convertType);

            return convertingBody;
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