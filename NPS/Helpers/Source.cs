using NPS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[System.Serializable]
public class Source
{
    public string uri;
    public DatabaseType type;

    public Source(string uri, DatabaseType type)
    {
        this.uri = uri;
        this.type = type;
    }

    public override string ToString()
    {
        return type.ToString();
    }
}
