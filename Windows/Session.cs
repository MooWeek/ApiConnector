using System;

namespace oda
{
    internal class Session : IDisposable
    {
        private bool IsDisposed = false;

        /// <summary>
        /// Запускает процесс отправки запроса к сторонней БД
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        internal string ExecuteQuery(Request request)
        {
            if (IsDisposed) return null;

            return Messanger.SendRequest(request);
        }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }
}
