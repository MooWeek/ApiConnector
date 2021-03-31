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
        internal bool ExecuteQuery(Request request)
        {
            if (IsDisposed) return false;

            return Messanger.SendRequest(request);
        }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }
}
