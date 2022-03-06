using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1.Helper
{
    public class Response<T>
    {
        public bool IsSuccessfully { get; private set; }

        public HttpStatusCode HttpStatusCode { get; private set; }

        public string ReturnUrl { get; private set; }

        public int Count { get; private set; }

        public string ErrorMessage { get; private set; }

        public Exception Exception { get; private set; }

        public T Data { get; private set; }

        private Response()
        {

        }

        private static int count(T data)
        {
            int count = 0;

            if (data != null)
                count = 1;

            ICollection col = data as ICollection;
            if (col != null)
                count = col.Count;

            return count;
        }

        public static Response<T> Success(T data = default, HttpStatusCode httpStatusCode = HttpStatusCode.OK, string returnUrl = null)
        {
            return new Response<T>
            {
                IsSuccessfully = true,
                Data = data,
                HttpStatusCode = httpStatusCode,
                ReturnUrl = returnUrl,
                Count = count(data),
            };
        }


        public static Response<T> Error(string errorMessage, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest)
        {
            return new Response<T>
            {
                IsSuccessfully = false,
                HttpStatusCode = httpStatusCode,
                ErrorMessage = errorMessage,
            };
        }
    }
}
