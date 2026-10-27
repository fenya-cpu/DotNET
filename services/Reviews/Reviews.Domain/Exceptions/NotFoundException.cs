using System;
using System.Collections.Generic;
using System.Text;

namespace Reviews.Domain.Exceptions
{
    public class NotFoundException : Exception
    {
        public NotFoundException(string name, object key)
            : base($"Ресурс \"{name}\" ({key}) не знайдено.") { }
    }
}
