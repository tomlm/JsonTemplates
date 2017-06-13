using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.Source
{

    public class Rootobject
    {
        public string str { get; set; }
        public int number { get; set; }
        public DateTime dt { get; set; }
        public Obj obj { get; set; }
        public Foo foo { get; set; }

        public Boolean boolean { get; set; }
    }


    public class Obj
    {
        public string X { get; set; }
        public int Y { get; set; }
    }

    public class Foo
    {
        public Bar[] bar { get; set; }
    }

    public class Bar
    {
        public string Name { get; set; }
        public int Rating { get; set; }
        public Subobject subObject { get; set; }
    }

    public class Subobject
    {
        public int x { get; set; }
    }

}
namespace UnitTests.Expected
{

    public class Rootobject
    {
        public string stringBinding { get; set; }
        public string compoundBinding { get; set; }
        public int numberBinding { get; set; }
        public string noBinding { get; set; }
        public DateTime dateBinding { get; set; }
        public Subobjectbinding subObjectBinding { get; set; }
        public Item[] items { get; set; }
    }

    public class Subobjectbinding
    {
        public string title { get; set; }
    }

    public class Item
    {
        public int num { get; set; }
        public Metadata metadata { get; set; }
    }

    public class Metadata
    {
        public string title { get; set; }
    }

}