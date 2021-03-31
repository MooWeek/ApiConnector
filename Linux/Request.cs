using System;
using System.Text;
using System.Net.Http;

namespace oda
{
    internal class Request
    {


        private string _url = String.Empty;
        private Class targetClass = null;

        private readonly HttpClientHandler clientHendler = new HttpClientHandler();
        private readonly HttpClient httpClient = null;
        private HttpContent httpContent = null;

        public int count = 0;


        #region Properties
        /// <summary>
        /// Объект-источник значений класса APIConnector
        /// </summary>
        internal Object TemplateObject
        {
            get;
            set;
        }

        internal HttpClient HttpClient
        {
            get { return httpClient; }
        }

        /// <summary>
        /// Копия обїекта от которого запущен метод
        /// </summary>
        internal xmlDocument SourceXmlDocument
        {
            get;
            set;
        }

        /// <summary>
        /// Объект, от которого запущен метод
        /// </summary>
        internal Object SourceObject
        {
            get;
            set;
        }

        /// <summary>
        /// Возвращает таблицу параметров из SourceObject
        /// </summary>
        internal xmlElement TemplateElement
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
                targetClass = SourceObject.Class;
            }
        }


        public HttpContent HttpContent 
        {
            get {
            //    if (httpContent == null)
            //        return CreateHttpContent();
            //    else
                    return httpContent;
            }
            set
            {
                httpContent = value;
            }
        }


        internal HttpContent DataContent 
        {
            get;
            set;
        }

        public string ContentType
        {
            get;
            set;
        }

        public string MethodType 
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
                    string apiUrl = TemplateElement.GetAttribute("Returns");
                    string mainUrl = TemplateElement.GetAttribute("MethodName");
                    if (!string.IsNullOrEmpty(apiUrl))
                    {
                        _url = mainUrl + SourceXmlDocument.Root.XQuery(apiUrl);
                    }
                    else
                    {
                        _url = mainUrl;
                    }
                }                
                return _url;
            }
        }
        #endregion

        internal Request(Object sourceObject, Object templateObject, xmlDocument sourceXmlDocument, xmlElement templateElem)
        {

            clientHendler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
            httpClient = new HttpClient(clientHendler);

            TemplateObject = templateObject;
            SourceObject = sourceObject;
            SourceXmlDocument = sourceXmlDocument;
            TemplateElement = templateElem;


            ContentType = TemplateElement.GetAttribute("ContentType");
            MethodType = TemplateElement.GetAttribute("MethodDispatch");

            SetHeaders();
        }

        public void SetHeaders() 
        {
            xmlNodeList HeadersList = TemplateElement.SelectNodes("HeadersList");
            foreach (xmlElement Header in HeadersList)
            {
                string HeaderName = Header.GetAttribute("Name");
                string HeaderXq = Header.GetAttribute("Value");
                string HeaderValue = SourceXmlDocument.Root.XQuery(HeaderXq);
                httpClient.DefaultRequestHeaders.Add(HeaderName, HeaderValue);
            }
        }


        public void SetData(string dataString)
        {

            DataContent = new StringContent(dataString, Encoding.UTF8, ContentType);
        }

        public HttpContent SetMultipartFormData()
        {            
            MultipartFormDataСreator MFDT = new MultipartFormDataСreator(SourceObject, TemplateElement);

            return MFDT.DataGeneration();
        }
    }
}