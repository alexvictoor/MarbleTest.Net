using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarbleTest.Net
{
    public interface ICheck
    {
        void ToBe(string marble, 
            object values, 
            Exception errorValue = null);
    }
}
