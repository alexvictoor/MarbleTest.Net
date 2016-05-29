using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarbleTest.Net
{
    public interface ISetupSubscriptionsTest
    {
        void ToBe(params string[] marbles);
    }
}
