var src = File.ReadAllText("Example.oml");
var expr = new Parse {
	src = src,
}.ParseExpr();
Console.WriteLine();
class Parse {
	public string src;
	public int ind;
	public bool valid => ind < src.Length;
	public char cur => src[ind];
	public void inc () => ind++;
	public object ParseExpr (string lhs = null) {
		Do:
		if(valid) {
			switch(cur) {
				case '{':
					return ParseStruct(lhs);
				case '"':
					return ParseString();
				case ' ' or '\t' or '\r' or '\n':
					inc();
					goto Do;
				default:
					var sym = ParseSymbol();
					return ParseExpr(sym);
			}
		}
		throw new Exception("EOF");
	}
	public OmlStruct ParseStruct (string tag = null) {
		inc();
		var result = new OmlStruct { tag = tag };
		var content = new List<object>();
		result.keys["content"] = content;
		Do:
		if(valid) {
			switch(cur) {
				case '}':
					inc();
					return result;
				case '"':
					var str = ParseString();
					switch(cur) {
						case ':':
							inc();
							result.keys[str] = ParseExpr();
							goto Do;
					}
					goto Do;
				case '{':
					content.Add(ParseStruct());
					goto Do;
				case ' ' or '\t' or '\r' or '\n':
					inc();
					goto Do;
				default:
					var key = ParseSymbol();
					switch(cur) {
						case ':':
							inc();
							result.keys[key] = ParseExpr();
							goto Do;
						default:
							content.Add(key);
							goto Do;
					}
			}
		}
		throw new Exception("EOF error");
	}
	public string ParseSymbol() {
		var result = "";
		Do:
		switch(cur) {
			case ':':
			case '{':
			case ' ' or '\t' or '\r' or '\n':
			case '"':
				if(result.Length == 0) throw new Exception();
				return result;
			
			default:
				result += cur;
				inc();
				goto Do;
		}
	}
	public string ParseString() {
		inc();
		var result = "";
		Do:
		if(valid) {
			switch(cur) {
				case '"':
					inc();
					return result;
				default:
					result += cur;
					inc();
					goto Do;
			}
		}
		throw new Exception("EOF");
	}
}
class OmlStruct {
	public string tag;
	public Dictionary<string, object> keys = new();
}