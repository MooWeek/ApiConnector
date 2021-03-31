using System;
using System.IO;
using System.Net.Http;
using System.Timers;

namespace oda
{
    class MultipartFormDataСreator
    {
        private readonly Object sourceObject;
        private readonly xmlElement template;
        private bool waitFileInBase = true;
        private bool error = false;
        private xmlElement fileElement;

        public MultipartFormDataСreator(Object sourceObject, xmlElement template)
        {
            this.sourceObject = sourceObject;
            this.template = template;
        }

        /// <summary>
        /// Генерация данных MultipartFormData
        /// </summary>
        /// <returns>Конвертированые данные запроса HttpContent</returns>
        public HttpContent DataGeneration()
        {
            MultipartFormDataContent multipartFormDataContent = new MultipartFormDataContent();

            try
            {
                WriteTextParams(multipartFormDataContent);

                WriteFileParams(multipartFormDataContent);
            }
            catch(Exception ex)
            {
                Messages.showMessage("АпиКоннектор: Не удалось сформировать данные MultipartFormData. Объект - " + sourceObject.FullId + "  Ошибка: " + ex.Message);
            }
            return multipartFormDataContent;
        }


        /// <summary>
        /// Записывает данные запроса с тыпом Text
        /// </summary>
        /// <param name="writingStream">Поток для записи данных</param>
        private void WriteTextParams(MultipartFormDataContent multipartFormDataContent)
        {
            using xmlNodeList textList = template.SelectNodes("FormDatas[@Type='Text']");
                foreach (xmlElement textParam in textList)
                {
                    string textKey = GetParamKey(textParam);
                    string textXQValue = sourceObject.XQuery(GetParamValue(textParam));

                    multipartFormDataContent.Add(new StringContent(textXQValue), textKey);
                }
        }


        /// <summary>
        /// Записывает данные запроса с тыпом File
        /// </summary>
        /// <param name="writingStream">Поток для записи данных</param>
        private void WriteFileParams(MultipartFormDataContent multipartFormDataContent)
        {
            using xmlNodeList fileList = template.SelectNodes("FormDatas[@Type='File']");
                foreach (xmlElement fileParam in fileList)
                {
                    string fileKey = GetParamKey(fileParam);
                    string fileXQValue = sourceObject.XQuery(GetParamValue(fileParam));

                    if (string.IsNullOrEmpty(fileXQValue))
                        continue;

                    SetFiles(fileXQValue, fileKey, multipartFormDataContent);
                }
        }

        /// <summary>
        /// Установка файлов в данные запроса
        /// </summary>
        /// <param name="fileList">Список файлов</param>
        /// <param name="fileKey">Ключ для параметров запроса</param>
        /// <param name="multipartFormDataContent">Поток для записи данных</param>
        private void SetFiles(string fileList, string fileKey, MultipartFormDataContent multipartFormDataContent)
        {
            if (sourceObject == null || sourceObject.IsDisposed)
                throw new Exception("АпиКоннектор: Не удалось загрузить список файлов для отправки. Не установлен объект-контекст отправки");

            if (string.IsNullOrEmpty(fileList))
                return;

            xmlDocument fileListDoc = new xmlDocument(fileList);
            foreach (xmlElement node in fileListDoc.Root.ChildElements)
            {
                try
                {
                    fileElement = node;
                    string filePath = GetFilePath(fileKey, multipartFormDataContent);
                }
                catch (Exception e)
                {
                    Messages.showException(e);
                }
            }
        }

        /// <summary>
        /// Поиск файла, запись его в данные запроса и получение пути
        /// </summary>
        /// <param name="fileKey">Ключ для параметров запроса</param>
        /// <param name="multipartFormDataContent">Поток для записи данных</param>
        /// <returns></returns>
        private string GetFilePath(string fileKey, MultipartFormDataContent multipartFormDataContent)
        {
            File file;

            file = IsFileLibrary() ? GetFileFromFileLibrary() : GetFileFromObject();

            if (file == null)
                throw new Exception("АпиКоннектор: Не удалось найти файл!");

            string filePath = file.Load();

            if (string.IsNullOrEmpty(filePath))
                filePath = file.Path;

            string fileName = filePath.Split('/')[^1];

            if (!TryGetFile(fileKey, fileName, multipartFormDataContent, file))
            {
                Messages.showMessage("АпиКоннектор: Не удалось загрузить файл.");
            }

            return filePath;
        }

        /// <summary>
        /// Поиск файла типа (File)
        /// </summary>
        /// <returns>Найденый файл</returns>
        private File GetFileFromObject()
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
        /// <returns>Найденый файл</returns>
        private File GetFileFromFileLibrary()
        {
            DataField fileDataField = GetFileDataFieldFromLink(out Object fileLibraryObject);
            if (fileDataField == null)
                throw new Exception("АпиКоннектор: Не найдено поле file в библиотеке файлов");

            string fileInfo = fileDataField.loadFile(fileLibraryObject.Root);

            if (string.IsNullOrEmpty(fileInfo))
                throw new Exception("АпиКоннектор: Не найден файл в объекте бибилиотеки файлов");

            return new File(fileInfo, fileLibraryObject.Dir);
        }

        /// <summary>
        /// Получение поля с данными по ссылке
        /// </summary>
        /// <param name="fileLibraryObject"></param>
        /// <returns></returns>
        private DataField GetFileDataFieldFromLink(out Object fileLibraryObject)
        {
            string libraryObjectLink = fileElement.XQuery("xs:string(@link)");

            if (string.IsNullOrEmpty(libraryObjectLink))
                throw new Exception("АпиКоннектор: Ссылка на объект бибилиотеки файлов не установленна!");

            fileLibraryObject = sourceObject.Class.GetClass(libraryObjectLink[0..^18]).GetObject(libraryObjectLink[^15..]);

            if (fileLibraryObject == null)
                throw new Exception("АпиКоннектор: Не найден объект бибилиотеки файлов - " + libraryObjectLink);

            if (fileElement.XQuery("xs:string(@cnm)") == "FileLibrary")
                return fileLibraryObject.GetDataField("file");
            else
                return fileLibraryObject.GetDataField("icon");
        }

        /// <summary>
        /// Получение Ключ данных
        /// </summary>
        /// <param name="Param">Входящие параметры</param>
        /// <returns>Ключ данных</returns>
        private string GetParamKey(xmlElement Param) { 
            return Param.GetAttribute("Key");
        }

        /// <summary>
        /// Получение значения данных
        /// </summary>
        /// <param name="Param">Входящие параметры</param>
        /// <returns>Значения данных</returns>
        private string GetParamValue(xmlElement Param)
        {
            return Param.GetAttribute("ValueXQ");
        }

        /// <summary>
        /// Проверка на тип файла(File or FileLibrary)
        /// </summary>
        /// <returns>Равен ли тип файла - FileLibrary или ImageLibrary</returns>
        private bool IsFileLibrary()
        {
            return (fileElement.XQuery("xs:string(@cnm)") == "FileLibrary" || fileElement.XQuery("xs:string(@cnm)") == "ImageLibrary");
        }

        private bool TryGetFile(string fileKey, string fileName, MultipartFormDataContent multipartFormDataContent, File file)
        {
            try
            {
                if (file == null)
                {
                    Messages.showMessage("Запуск таймера" );
                    FileTimer timer = new FileTimer
                    {
                        FileName = fileName,
                        SourceObject = sourceObject,
                        UsedCount = 0,
                        Interval = 5000
                    };
                    timer.Elapsed += new ElapsedEventHandler(Timer_Elapsed);
                    waitFileInBase = true;
                    error = false;
                    timer.Start();
                    while (waitFileInBase && !error && timer.UsedCount < 10) { }
                    
                    timer = null;

                    if (error)
                        throw new Exception("Не удалось загрузить файл - " + fileName);

                    Messages.showMessage("Таймер обнулен");

                    if (!IsFileLibrary())
                        file = GetFileFromObject();
                    else
                        file = GetFileFromFileLibrary();
                }

                string filePath = file.RemoteFilePath;

                byte[] fileBytes;

                using (FileStream fs = new FileStream(@filePath, FileMode.Open, FileAccess.Read))
                {
                    fileBytes = new byte[fs.Length];
                    fs.Read(fileBytes, 0, fileBytes.Length);
                    fs.Close();
                    multipartFormDataContent.Add(new ByteArrayContent(fileBytes), fileKey, fileName);
                }
                return true;
            }
            catch(Exception ex)
            {
                Messages.showMessage(ex.Message);
                return false;
            }
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            FileTimer timer = sender as FileTimer;
            timer.Stop();

            File file;

            if (!IsFileLibrary())
                file = GetFileFromObject();
            else
                file = GetFileFromFileLibrary();

            timer.SourceObject.Class.ReloadClassFromServer();

            if (!IsFileLibrary())
                file = GetFileFromObject();
            else
                file = GetFileFromFileLibrary();

            if (10 <= timer.UsedCount)
            {
                Messages.showMessage("timer.UsedCount - " + timer.UsedCount);
                error = true;
                return;
            }

            if (file == null)
            {
                timer.IncrimentUsedCount();
                Messages.showMessage("Повторный запуск таймера. Количество выполненных повторов - " + timer.UsedCount);
                timer.Start();
            }
            else
            {
                timer.Elapsed -= new ElapsedEventHandler(Timer_Elapsed);
                timer.Dispose();
                waitFileInBase = false;
            }
        }
    }
}
