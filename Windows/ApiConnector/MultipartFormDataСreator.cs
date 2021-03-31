using System;
using System.IO;
using System.Text;

namespace oda
{
    class MultipartFormDataСreator
    {
        private readonly Object sourceObject;
        private readonly xmlElement template;
        private readonly string boundary;
        private readonly byte[] boundarybytes;

        byte[] fileBytes;

        public MultipartFormDataСreator(Object sourceObject, xmlElement template, string boundary)
        {
            this.sourceObject = sourceObject;
            this.template = template;
            this.boundary = boundary;
            boundarybytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");
        }

        /// <summary>
        /// Генерация данных MultipartFormData
        /// </summary>
        /// <returns>Конвертированые данные запроса в байтах</returns>
        public byte[] DataGeneration()
        {
            byte[] data;

            try
            {
                //Encoding
                byte[] boundarybytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

                using (Stream readStream = new MemoryStream())
                {
                    string formdataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";

                    byte[] headerbytes2 = Encoding.UTF8.GetBytes(string.Format(formdataTemplate, template.GetHashCode().ToString(), boundary));
                    readStream.Write(headerbytes2, 0, headerbytes2.Length);

                    WriteTextParams(readStream);

                    WriteFileParams(readStream);

                    byte[] trailer = Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
                    readStream.Write(trailer, 0, trailer.Length);

                    readStream.Position = 0;

                    data = new byte[readStream.Length];
                    readStream.Read(data, 0, data.Length);
                    readStream.Close();
                }
            }
            catch
            {
                Messages.showMessage("АпиКоннектор: Не удалось сформировать данные MultipartFormData. Объект - " + sourceObject.FullId);
                data = null;
            }
            return data;
        }

        /// <summary>
        /// Записывает данные запроса с тыпом Text
        /// </summary>
        /// <param name="writingStream">Поток для записи данных в байты</param>
        void WriteTextParams(Stream writingStream)
        {
            string textHeaderTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{2}";

            using (xmlNodeList textList = template.SelectNodes("FormDatas[@Type='Text']"))
            {
                foreach (xmlElement textParam in textList)
                {
                    string textKey = GetParamKey(textParam);
                    string textValue = GetParamValue(textParam);

                    writingStream.Write(boundarybytes, 0, boundarybytes.Length);

                    byte[] textPartbytes = Encoding.UTF8.GetBytes(string.Format(textHeaderTemplate, textKey, textValue));
                    writingStream.Write(textPartbytes, 0, textPartbytes.Length);
                }
            }
        }

        private string GetParamValue(xmlElement textParam)
        {
            return sourceObject.XQuery(textParam.GetAttribute("ValueXQ"));
        }

        private string GetParamKey(xmlElement textParam)
        {
            return textParam.GetAttribute("Key");
        }

        /// <summary>
        /// Записывает данные запроса с тыпом File
        /// </summary>
        /// <param name="writingStream">Поток для записи данных в байты</param>
        void WriteFileParams(Stream writingStream)
        {
            string fileHeaderTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";

            using (xmlNodeList fileList = template.SelectNodes("FormDatas[@Type='File' or @Type='FileLibrary']"))
            {
                foreach (xmlElement fileParam in fileList)
                {
                    string fileKey = GetParamKey(fileParam);
                    string fileValue = GetParamValue(fileParam);

                    if (string.IsNullOrEmpty(fileValue))
                        continue;

                    xmlDocument fileListDoc = new xmlDocument(fileValue);
                    foreach (xmlElement node in fileListDoc.Root.ChildElements)
                    {
                        try
                        {
                            string filePath = GetFilePath(node);
                            string fileName = filePath.Split('\\')[filePath.Split('\\').Length - 1];

                            writingStream.Write(boundarybytes, 0, boundarybytes.Length);
                            byte[] fileHeaderbytes = Encoding.UTF8.GetBytes(string.Format(fileHeaderTemplate, fileName, fileName, "application/octet-stream"));
                            writingStream.Write(fileHeaderbytes, 0, fileHeaderbytes.Length);

                            writingStream.Write(fileBytes, 0, fileBytes.Length);
                        }
                        catch (Exception e)
                        {
                            Messages.showException(e);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Поиск файла, запись его в данные запроса и получение пути
        /// </summary>
        /// <param name="fileElement">XML елемент с входными данными</param>
        /// <returns>Путь к файлу</returns>
        private string GetFilePath(xmlElement fileElement)
        {
            File file;

            if (!IsFileLibrary(fileElement))
                file = GetFileFromObject(fileElement);
            else
                file = GetFileFromFileLibrary(fileElement);

            if (file == null)
                throw new Exception("АпиКоннектор: Не удалось найти файл!");

            string filePath = file.Load();

            if (string.IsNullOrEmpty(filePath))
                filePath = file.Path;

            fileBytes = TryGetFileBytes(file, filePath);

            return filePath;
        }

        private bool IsFileLibrary(xmlElement fileElement)
        {
            return fileElement.XQuery("xs:string(@cnm)") == "FileLibrary" || fileElement.XQuery("xs:string(@cnm)") == "ImageLibrary";
        }

        /// <summary>
        /// Поиск файла типа (File)
        /// </summary>
        /// <param name="fileElement">XML елемент с входными данными</param>
        /// <returns>Найденый файл</returns>
        private File GetFileFromObject(xmlElement fileElement)
        {
            string fileName;
            string fileLink = fileElement.GetAttribute("link");

            if (string.IsNullOrEmpty(fileLink))
                fileName = fileElement.GetAttribute("file");
            else
                fileName = fileLink.Remove(0, fileLink.IndexOf("/") + 1);

            if (string.IsNullOrEmpty(fileName))
                throw new Exception("АпиКоннектор: Не указанно имя файла для отправки");

            return sourceObject.Dir.Files.getFile(fileName);
        }

        /// <summary>
        /// Поиск файла типа (FileLibrary, ImageLibrary)
        /// </summary>
        /// <param name="fileElement">XML елемент с входными данными</param>
        /// <returns>Найденый файл</returns>
        private File GetFileFromFileLibrary(xmlElement fileElement)
        {
            Object fileLibraryObject = GetLibraryObject(fileElement);
            
            if (fileLibraryObject == null)
                throw new Exception("АпиКоннектор: Не найден объект бибилиотеки файлов по заданому пути");

            string fileInfo = GetDataField(fileLibraryObject, fileElement).loadFile(fileLibraryObject.Root);

            if (string.IsNullOrEmpty(fileInfo))
                throw new Exception("АпиКоннектор: Не найден файл в объекте бибилиотеки файлов");

            return new File(fileInfo, fileLibraryObject.Dir);
        }

        private Object GetLibraryObject(xmlElement fileElement)
        {
            string fileLibraryObjectLink = fileElement.XQuery("xs:string(@link)");

            if (string.IsNullOrEmpty(fileLibraryObjectLink))
                throw new Exception("АпиКоннектор: Ссылка на объект бибилиотеки файлов не установленна!");

            return sourceObject.Class.GetClass(fileLibraryObjectLink.Substring(0, fileLibraryObjectLink.Length - 18)).GetObject(fileLibraryObjectLink.Substring(fileLibraryObjectLink.Length - 15));
        }

        private DataField GetDataField(Object fileLibraryObject, xmlElement fileElement)
        {
            DataField fileDataField;

            if (fileElement.XQuery("xs:string(@cnm)") == "FileLibrary")
                fileDataField = fileLibraryObject.GetDataField("file");
            else
                fileDataField = fileLibraryObject.GetDataField("icon");

            if (fileDataField == null)
                throw new Exception("АпиКоннектор: Не найдено поле file или icon в библиотеке файлов");

            return fileDataField;
        }

        /// <summary>
        /// Запись файла в байты
        /// </summary>
        /// <param name="file">Файл</param>
        /// <param name="filePath">Путь к Файлу</param>
        /// <returns>Файл записаный в байтах</returns>
        byte[] TryGetFileBytes(File file, string filePath)
        {
            if (file == null)
                throw new Exception("АпиКоннектор: Не удалось получить файл");

            byte[] fileBytes;
            using (FileStream fs = new FileStream(@filePath, FileMode.Open, FileAccess.Read))
            {
                fileBytes = new byte[fs.Length];
                fs.Read(fileBytes, 0, fileBytes.Length);
                fs.Close();
            }

            return fileBytes;
        }
    }
}
