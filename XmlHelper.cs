using System;
using System.Linq;
using System.Text;
using System.Xml;

namespace GemGuide;

public static class XmlHelper
{
    public static string FindXPath(this XmlNode node)
    {
        var builder = new StringBuilder();
        while (node != null)
        {
            switch (node.NodeType)
            {
                case XmlNodeType.Attribute:
                    builder.Insert(0, $"/@{node.Name}");
                    node = ((XmlAttribute)node).OwnerElement;
                    break;
                case XmlNodeType.Element:
                    var index = FindElementIndex((XmlElement)node);
                    builder.Insert(0, $"/{node.Name}[{index}]");
                    node = node.ParentNode;
                    break;
                case XmlNodeType.Document:
                    return builder.ToString();
                default:
                    throw new ArgumentException("Only elements and attributes are supported");
            }
        }

        throw new ArgumentException("Node was not in a document");
    }

    private static int FindElementIndex(XmlElement element)
    {
        var parentNode = element.ParentNode;
        if (parentNode == null)
        {
            return -1;
        }

        if (parentNode is XmlDocument)
        {
            return 1;
        }

        var parent = (XmlElement)parentNode;
        var index = 1;
        foreach (var candidate in parent.ChildNodes.OfType<XmlElement>())
        {
            if (candidate.Name == element.Name)
            {
                if (candidate == element)
                {
                    return index;
                }

                index++;
            }
        }

        throw new ArgumentException("Couldn't find element within parent");
    }
}