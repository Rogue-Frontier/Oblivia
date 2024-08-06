// See https://aka.ms/new-console-template for more information
using System.Linq.Expressions;
using System.Reflection.Metadata.Ecma335;

Console.WriteLine("Hello, World!");

var code = """

{
    Data: class {
        str: "Hello World",
        num: int(5)
    }
    main(args: string): int {
        a : "hello world",
        print(a),
        print(a:str): void {
            
        }


        b : 1,
        b := b + 1,

        data : {
            a : a,
            b : b,
            c : ^b
        },
        inc(u: int, v:int) : int {
            ^a := u + v
        },
        dict: {
            true: 5,
            false: 8
        },

        val: bool(true from dict),
        e(b:int): int(b = 1 & 122 | 3123),
        print() : {
        },
    }
}
""";
var tokenizer = new Tokenizer(code);
var tokens = new List<Token> { };
while(tokenizer.Next() is { type: not TokenType.EOF} t) {
    tokens.Add(t);
}

var parser = new Parser(tokens);
var obj = parser.NextObject();
return;
class Parser {
    int index;
    List<Token> tokens;

    public Parser(List<Token> tokens) {
        this.tokens = tokens;
    }

	void inc () => index++;
	//A:B,
	//A():B,
	//A:B {},
	//A():B {},
    //A(a:int, b:int): B{}

    //Change to NextDefinitionOrExpression
	public Element NextDefinition () {
		var name = tokens[index].value;
		inc();
		switch(currToken.type) {
			case TokenType.L_PAREN:
				inc();
                var par = NextParList();
                //inc();
				return new _DefineVal {
					key = name,
					value = NextExpression()
				};
			case TokenType.COLON:
				inc();
                return new _DefineVal {
                    key = name,
                    value = NextExpression()
				};
		}
        List<Element> NextParList () {
			var par = new List<Element> { };
            Check:
            switch(currToken.type) {
                case TokenType.R_PAREN:
                    inc();
                    return par;
                case TokenType.COMMA:
                    inc();
                    goto Check;
                case TokenType.NAME:
                    par.Add(NextPair());

                    goto Check;
            }
            return par;

            Element NextPair() {
				var key = currToken.value;
				inc();

                if(currToken.type == TokenType.COMMA || currToken.type == TokenType.R_PAREN) {
                    inc();
                    return new _DefineVal { key = key, value = null };
                }
                if(currToken.type != TokenType.COLON) {
                    throw new Exception($"Expected colon in parameter list: {currToken.type}");
                }
                inc();
                if(currToken.type != TokenType.NAME) {
                    throw new Exception($"Expected symbol in parameter list: {currToken.type}");
                }
                var result = new _DefineVal { key = key, value = new _Value<string> { value = currToken.value } };
                inc();
                return result;
			}
		}

        return null;
	}


    public Token currToken => tokens[index];
	public Element NextExpression () {
        switch(currToken.type) {
            case TokenType.NAME:
                return NextSymbolicExpression();
            case TokenType.STRING:
                return NextString();
            case TokenType.INTEGER:
                return NextInteger();
            case TokenType.L_CURLY:
                //Object literal
                return NextObject();
        }

        Element NextSymbolicExpression() {
			//May be cast object, variable, or a function call / literal.
			var name = currToken.value;
			inc();
			switch(currToken.type) {
				case TokenType.L_PAREN:
					inc();

                    var args = NextArgList();
					return new CallFunc { name = name, args = args };
				case TokenType.L_CURLY:
					return new _CastObject { type = name, obj = NextObject() };
				case TokenType.COMMA:
					//Symbol
					return null;
			}
            throw new Exception();

            List<Element> NextArgList () {

                List<Element> items = [];
                Check:
                switch(currToken.type) {
                    case TokenType.INTEGER:
                        items.Add(new _Value<int> { value = int.Parse(currToken.value) });
                        inc();
                        goto Check;
                    case TokenType.R_PAREN:
                        inc();
                        return items;
                }
                throw new Exception($"Unexpected token in arg list: {currToken.type}");
			}
		}

        throw new Exception($"Unexpected token in expression: {currToken.type}");
	}
    public _Value<string> NextString () {
        var value = tokens[index].value;
        inc();
        return new _Value<string> { value = value };
    }

    public _Value<int> NextInteger() {
        var value = int.Parse(tokens[index].value);
        inc();
        return new _Value<int> { value = value };
    }

	public _Object NextObject() {
        inc();
		var ele = new List<Element>();
        Check:
        switch(currToken.type) {
            case TokenType.NAME:
                ele.Add(NextDefinition());
                goto Check;
            case TokenType.COMMA:
                inc();
                goto Check;
            case TokenType.R_CURLY:
                inc();
                return new _Object { items = ele };
		}

        throw new Exception($"Unexpected token in object expression: {currToken.type}");
	}
}
public class _EOF : Element {

}

public class _CastObject : Element {
    public string type;
    public _Object obj;
}

public class CallFunc : Element {
    public string name;
    public List<Element> args;
}
public class _Object : Element {
    public List<Element> items;
}
public class _Value : Element { }
public class _Value<T> : Element {
    public T value;
}
public class _DefineVal : Element {
    public string key;
    public Element value;
}
public class _DefineFunc : Element {
	public string key;
	public Element value;
}

public class _Reassign : Element {

}
public interface Element {

}

class Tokenizer {
    string src;
    int index;
    public Tokenizer(string src) {
        this.src = src;
    }

    public Token Next() {
        if(index >= src.Length) {
            return new Token { type = TokenType.EOF };
        }

        var str = (params char[] c) => string.Join("", c);

        Check:
        var c = src[index];
        if(new Dictionary<char, TokenType> {
            ['='] = TokenType.EQUAL,
            [':'] = TokenType.COLON,
            ['('] = TokenType.L_PAREN,
            [')'] = TokenType.R_PAREN,
            ['['] = TokenType.L_SQUARE,
            [']'] = TokenType.R_SQUARE,
            ['{'] = TokenType.L_CURLY,
            ['}'] = TokenType.R_CURLY,
            [','] = TokenType.COMMA,
            ['^'] = TokenType.CARET,

            ['?'] = TokenType.QUESTION,
            ['!'] = TokenType.EXCLAIM,


            ['+'] = TokenType.PLUS,
            ['/'] = TokenType.SLASH,
        }.TryGetValue(c, out var tt)) {
            index += 1;
			return new Token { type = tt, value = str(c) };
		}

		switch(c) {
            case ' ' or '\r' or '\t' or '\n':
                index++;
                goto Check;
            case (>= 'a' and <= 'z') or (>= 'A' and <= 'Z'): {
					int dest = index + 1;
					while(dest < src.Length && src[dest] is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z')) {
						dest += 1;
					}
                    var v = src[index..dest];
                    index = dest;
					return new Token { type = TokenType.NAME, value = v };
				}
			case '"': {
                    int dest = index + 1;
                    while(dest < src.Length && src[dest] != '"') {
                        dest += 1;
                    }
                    dest += 1;

                    var v = src[index..dest];
                    index = dest;
                    return new Token { type = TokenType.STRING, value = v };
                }
            case >= '0' and <= '9': {
                    int dest = index + 1;
                    while(dest < src.Length && src[dest] is >= '0' and <= '9') {
                        dest += 1;
                    }

                    var v = src[index..dest];
                    index = dest;
                    return new Token { type = TokenType.INTEGER, value = v };
                }
        }

        throw new Exception();
    }
}
public enum TokenType {
	NAME,
    COMMA,
	COLON,
	L_PAREN,
	R_PAREN,
    L_CURLY,
    R_CURLY,
    L_SQUARE,
    R_SQUARE,
	CARET,
    EQUAL,
	STRING,
	INTEGER,
    PLUS,
    SLASH,

    QUESTION, EXCLAIM,

	EOF
}
class Token {
    public TokenType type;
    public string value;

    public string ToString () => $"[{type}] {value}";
}