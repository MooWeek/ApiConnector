using System;
using System.Net;
using System.IO;
using System.Text;

namespace oda
{
    internal class Request
    {
        private string _url = String.Empty;
        private Class targetClass = null;
        private HttpWebRequest httpWebRequest = null;
        public int count = 0;


        #region Properties
        /// <summary>
        /// Объект-источник значений класса APIConnector
        /// </summary>
        internal Object SourceObject
        {
            get;
            set;
        }

        /// <summary>
        /// Объект, от которого запущен метод
        /// </summary>
        internal Object TargetObject
        {
            get;
            set;
        }

        /// <summary>
        /// Возвращает таблицу параметров из SourceObject
        /// </summary>
        internal xmlElement SourceElement
        {
            get;
            set;
        }

        /// <summary>
        /// Класс, от которого запущен метод
        /// </summary>
        internal Class TargetClass
        {
            get 
            {
                return targetClass;
            }
            set 
            {
                targetClass = TargetObject.Class;
            }
        }

        /// <summary>
        /// Возвращает значение, указывающее на наличие данных в запросе
        /// </summary>
        internal bool HasData
        {
            get
            {
                return Data != null;
            }
        }

        /// <summary>
        /// Веб запрос
        /// </summary>
        public HttpWebRequest WebRequest
        {
            get
            {
                if (httpWebRequest == null)
                    return CreateHttpWebRequest();
                else
                    return httpWebRequest;
            }
            set
            {
                httpWebRequest = value;
            }
        }

        /// <summary>
        /// Возвращает или устанавливает данные для POST-запроса
        /// </summary>
        internal byte[] Data
        {
            get;
            set;
        }
        internal string DataString
        {
            get;
            set;
        }
        internal bool UseHasher
        {
            get;
            set;
        }

        public string ContentType
        {
            get;
            set;
        }

        /// <summary>
        /// Возвращает или устанавливает адрес
        /// </summary>
        internal string URL
        {
            get
            {
                if (String.IsNullOrEmpty(_url))
                {
                    string apiUrl = SourceElement.GetAttribute("Returns");
                    string mainUrl = SourceElement.GetAttribute("MethodName");

                    if (!string.IsNullOrEmpty(apiUrl))
                        _url = mainUrl + TargetObject.XQuery(apiUrl);
                    else
                        _url = mainUrl;
                }                
                return _url;
            }
        }
        #endregion

        internal Request(Object sourceObject, Object targetObject, xmlElement dataElem)
        {
            SourceObject = sourceObject;
            TargetObject = targetObject;
            SourceElement = dataElem;
        }

        /// <summary>
        /// Создание запроса
        /// </summary>
        /// <returns>Созданый запрос</returns>
        private HttpWebRequest CreateHttpWebRequest()
        {
            HttpWebRequest webRequest = (HttpWebRequest)System.Net.WebRequest.Create(URL);
            SetWebRequestPriperties(webRequest);

            SetContentType(webRequest);
            SetHeaders(webRequest);

            WebRequest = webRequest;

            return webRequest;
        }

        private void SetWebRequestPriperties(HttpWebRequest webRequest)
        {
            webRequest.KeepAlive = true;
            webRequest.Method = SourceElement.GetAttribute("MethodDispatch");
            webRequest.Timeout = Utils.StringToInt(SourceObject.Root.GetAttribute("Timeout"));
            webRequest.Credentials = CredentialCache.DefaultCredentials;
        }

        private void SetContentType(HttpWebRequest webRequest)
        {
            string contentType = SourceElement.GetAttribute("ContentType");

            if (contentType == "multipart/form-data")
            {
                string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
                webRequest.ContentType = "multipart/form-data; boundary=" + boundary;
                SetMultipartFormData(boundary);
            }
            else
                webRequest.ContentType = contentType;

            ContentType = contentType;
        }

        private void SetHeaders(HttpWebRequest webRequest)
        {
            xmlNodeList HeadersList = SourceElement.SelectNodes("HeadersList");
            foreach (xmlElement Header in HeadersList)
            {
                string HeaderName = Header.GetAttribute("Name");
                string HeaderValue = TargetObject.XQuery(Header.GetAttribute("Value"));
                webRequest.Headers.Add(HeaderName, HeaderValue);
            }
        }

        /// <summary>
        /// Запись данных запросав
        /// </summary>
        /// <param name="dataString">Данные запроса</param>
        public void SetData(string dataString)
        {
            DataString = dataString;
            Data = Encoding.UTF8.GetBytes(dataString);
        }

        /// <summary>
        /// Запись данных MultipartFormData запросов
        /// </summary>
        /// <param name="boundary">разделитель</param>
        private void SetMultipartFormData(string boundary)
        {            
            MultipartFormDataСreator MFDT = new MultipartFormDataСreator(TargetObject, SourceElement, boundary);     
            Data = MFDT.DataGeneration();
        }
        
        internal HttpWebResponse GetResponse()
        {
            return (HttpWebResponse)WebRequest.GetResponse();
        }

        internal Stream GetRequestStream()
        {
            return WebRequest.GetRequestStream();
        }
    }
}