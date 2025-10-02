using System.Xml.Linq;
namespace Oblivia {
	public interface Node {
        XElement ToXML () => new(GetType().Name);
        string Source => "";
        object Eval (IScope ctx);
    }
}