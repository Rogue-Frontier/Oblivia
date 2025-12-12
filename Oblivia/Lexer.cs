using System.Data;
namespace Oblivia {
	public class Lexer {
        string src;
        int index;
        public Lexer (string src) {
            this.src = src;
        }
        public List<IToken> GetAllTokens () {
            var tokens = new List<IToken> { };
            while(Next() is { type: not TokenType.eof } t) {
                tokens.Add(t);
            }
            for(int i = 0; i < tokens.Count; i++) {
                var g = (int n) => tokens.Skip(i).Take(n).Select(t => t.type).ToArray();
                if(g(2) is [TokenType.minus, TokenType.angle_r]) {
                    tokens.RemoveRange(i, 2);
                    tokens.Insert(i, new Token { type = TokenType.arrow_e });
                    continue;
                }
                if(g(2) is [TokenType.angle_l, TokenType.minus]) {
					tokens.RemoveRange(i, 2);
					tokens.Insert(i, new Token { type = TokenType.arrow_w });
                    continue;
				}
				if(g(2) is [TokenType.colon, TokenType.equal]) {
					tokens.RemoveRange(i, 2);
					tokens.Insert(i, new Token { type = TokenType.coloneqq });
					continue;
				}
			}
            return tokens;
		}
		int row = 0;
		public IToken Next () {
			string str (params char[] c) => string.Join("", c);
			void inc () => index += 1;
			Check:
			if(index >= src.Length) {
                return new Token { type = TokenType.eof };
            }
            var c = src[index];
            switch(c) {
				/*
				case '?': {
						//String interpolation
						if(src[index+1] == '"') {
							inc();
							inc();
							List<IToken> tree = [];
							var text = "";
							while(src[index] != '"') {
								tree.Add(Next());
							}
							inc();
							return new TreeToken { tree = tree };
						}
						break;
					}
					*/
                case '/': {
                        if(src[index + 1] == '/') {
                            var start = index;
                            inc();
                            inc();
							while(index < src.Length && src[index] != '\n') {
								inc();
							}
                            var st = src[start..index];
							return new StrToken { str = st, src = st, type = TokenType.comment };
                        } else if(src[index + 1] == '*') {
                            var start = index;
                            inc();
                            inc();
							bool checkStop = false;
							while(index < src.Length) {
								if(src[index] == '*') {
									inc();
									checkStop = true;
								} else if(src[index] == '/' && checkStop) {
									inc();
                                    var st = src[start..index];
									return new StrToken { str = st, src = st, type = TokenType.comment };
									goto Check;
								} else {
									inc();
									checkStop = false;
								}
							}
                            {
                                var st = src[start..index];
                                return new StrToken { str = st, src = st, type = TokenType.comment };
                            }
							goto Check;
                        }
                        break;
                    }
                    /*
                case ('#'): {

                        var start = index;
                        inc();
                        /*
                        if(src[index] == '<') {
                            inc();

                            bool checkStop = false;
                            while(index < src.Length) {
								if(src[index] == '>' && checkStop) {
									inc();
									goto Check;
								} else if(src[index] == '#') {
                                    inc();
                                    checkStop = true;
                                } else {
                                    inc();
									checkStop = false;
								}
                            }
                            goto Check;
                        }
                        *//*
                        while(index < src.Length && src[index] != '\n') {
                            inc();
                        }
                        return new StrToken { str = src[start..index], type = TokenType.comment };
                    }
                case ('~'): {
                        inc();
                        while(index < src.Length && src[index] != '~') {
                            inc();
                        }
                        inc();
                        goto Check;
                    }
                    */
                case ('"'): {
                        int dest = index + 1;
                        var st = "";
                        while(dest < src.Length) {
                            if(src[dest] == '\\') {
								//var ss = src[(dest - 10)..(dest + 10)];
								dest += 1;
                                var ch = src[dest];
								st += ch switch {
                                    'r' => '\r',
                                    'n' => '\n',
                                    't' => '\t',
                                    '\\' => '\\',
                                    '\"' => '\"',
									'\'' => '\'',
								};
                                dest++;
							} else if(src[dest] == '"') {
                                break;
                            } else {
                                st += src[dest];
                                dest += 1;
                            }
                        }
                        dest += 1;
                        index = dest;
                        return new StrToken { type = TokenType.str, str = st, src=$"\"{st}\"" };
                    }
                    /*
                case '\t':
                    throw new Exception("Illegal token");
                    */
                case (' ' or '\r' or '\n' or '\t'): {
                        if(c == '\n') {
                            row++;
                        }
                        var st = $"{c}";
                        inc();
                        return new StrToken { str = st, src = st, type = TokenType.space };
                    }
                case (>= '0' and <= '9'): {
                        int dest = index;
						int val = 0;
                        var measure = "";
						Read:
                        if(dest < src.Length) {
                            var ch = src[dest];
							switch(ch) {
                                case(>= '0' and <= '9'):
                                    val = val * 10 + (ch - '0');
                                    dest++;
                                    goto Read;
                                case '_':
                                    dest++;
                                    goto Read;
                                case var ch_ when char.IsAsciiLetter(ch_):
                                    measure += ch;
                                    dest++;
                                    goto Read;
							}
						}
						var s = src[index..(dest)];
						index = dest;
                        return new MeasureToken { val = val, measure = measure };
                    }
                case ((>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or '_' or (>= '0' and <= '9')): {
                        int dest = index;
                        bool escape = false;
                        string v = "";
                        Read:
                        if(dest < src.Length) {
                            var ch = src[dest];
							switch(ch) {
                                case (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or '_' or (>= '0' and <= '9'):
                                    v += ch;
                                    dest += 1;
                                    goto Read;
                                case '\\':
                                    dest += 1;
                                    escape = true;
                                    goto Read;
                                default:
                                    if(escape) {
										v += ch;
										escape = false;
                                        dest += 1;
                                        goto Read;
                                    } else {
                                        goto Done;
                                    }
							}
                        }
                        Done:
                        index = dest;
                        return new StrToken { type = TokenType.name, str = v, src = v };
                    }
            }
			if(Enum.IsDefined(typeof(TokenType), (ulong)c)) {
				var _t = (TokenType)c;
                inc();
				return new Token { type = _t };
			}
            throw new Exception($"Unknown token {c} at row {row}, index {index}: {src[(index - 5)..(index + 5)]}");
		}
	}
	public enum TokenType : ulong {
		comma = ',',
		colon = ':',
		block_l = '{', block_r = '}',
		tuple_l = '(', tuple_r = ')',
		array_l = '[', array_r = ']',
		angle_l = '<', angle_r = '>',
		brack_l = '⟨', brack_r = '⟩',
		i_beam = '⌶',
		//•
		square_brack_l = '⟦',
		square_brack_r = '⟧',
		kronecker_product = '⊗',
		double_brack_l = '⟪',
		double_brack_r = '⟫',
		shell_brack_l = '⟬',
		shell_brack_r = '⟭',
		flat_paren_l = '⟮',
		flat_paren_r = '⟯',

		index_ascend = '⍋',
		index_descend = '⍒',
		//←

		log = '⍟',

		caret = '^',
		period = '.',
		equal = '=',
		plus = '+',
		minus = '-',
		slash = '/',
		quote = '\'',
		at = '@',
		question = '?',
		bang = '!',
		star = '*',
		pipe = '|',
		amp = '&',
		cash = '$',
		percent = '%',
		hash = '#',
		repeat = '¨',
		times = '×',
		divide = '÷',
		range = '↕',
		divides = '∣',
		ceil = '⌈',
		floor = '⌊',
		ellipsis = '…',
		h_ellipsis = '⋯',
		v_ellipsis = '⋮',
		ne_ellipsis = '⋰',
		se_ellipsis = '⋱',
		numero = '№',
		irony = '⸮',
		reference = '※',
		asterism = '⁂',
		radical = '√',
		guillemet_l = '‹',
		guillemet_r = '›',
		guillemet_ll = '«',
		guillemet_rr = '»',
		exponent = '⋆',
		manicule = '☞',
		pilcrow = '¶',
		copyright = '©',

		registered = '®',
		interpunct = '·',
		bullet = '•',
		double_hyphen = '⹀',
		double_oblique_hyphen = '⸗',

		before = '≺',
		after = '≻',
		before_eq = '≽',
		after_eq = '≼',

		inv_question = '¿',

		neq = '≠',
		approx_eq = '≈',
		equiv = '≡', not_equiv = '≢',
		INF = '∞',
		integral = '∫',
		SQRT = '√',
		bullet_op = '∙',
		sum = '∑',
		product = '∏',
		increment = '∆',
		geq = '≥',
		leq = '≤',

		gt = '>',
		lt = '<',

		arrow_w = '←',
		arrow_e = '→',
		arrow_s = '↓',
		arrow_n = '↑',
		cross_product = '⨯',

		HOUSE = '⌂',
		degree = '°',
		BULLET = '•',
		section = '§',
		lozenge = '◊',
		circle = '○',
		compose = '∘',
		inv_bang = '¡',
		triangle_n = '▲',
		triangle_s = '▼',
		small_triangle_n = '▴',
		small_triangle_e = '▸',
		small_triangle_s = '▾',
		small_triangle_w = '◂',
		PTR_R = '►',
		PTR_L = '◄',
		interrobang = '‽',
		double_bang = '‼',


		first = '⋖',
		last = '⋗',

		member_of = '∈',
		not_member_of = '∉',

		empty = '∅',

		double_plus = '⧺',
		//count = '⧣',

		is_subset_eq = '⊆',
		has_subset_eq = '⊇',
		is_subset = '⊂',
		has_subset = '⊃',

		delta = 'Δ',

		for_all = '∀',
		exists = '∃',
		not_exists = '∄',
		//∄

		nvdash = '⊬',
		nvDash = '⊭',
		diamond = '◇',
		coloneqq = '≔',
		triangleq = '≜',
		def = '≝',
		strictif = '⥽',

		ulcorner = '⌜',
		urcorner = '⌝',
		nexists = '∄',
		nand_ = '⊼',
		nor_ = '⊽',
		odot = '⊙',
		left_right_tack = '⟛',
		models = '⊧',
		forces = '⊩',
		never = '⟡',
		was_never = '⟢',
		will_never = '⟣',
		was_always = '⟤',
		will_always = '⟥',
		reverse_not = '⌐',
		and_and = '⨇',

		implies = '⇒',
		iff = '⇔',

		maps_to = '↦',

		is_proportional_to = '∝',
		such_that = '∋',
		dice = '⚄',
		deal = '⧂',

		therefore = '∴',
		because = '∵',

		yes = '⟙',
		no = '⟘',

		count = '⌗',
		loopedsquare = '⌘',

		position_indicator = '⌖',


		proves = '⊢',
		entails = '⊨',

		not = '¬',


		//∩ ∪
		//⋂ ⋃	
		//and = '⋀',
		//or = '⋁',

		or = '∨',
		and = '∧',
		nor = '⍱',
		nand = '⍲',
		xor = '⊻',

		square_fill_l = '◧',
		square_fill_r = '◨',


		//
		intersection = '⋂',
		union = '⋃',

		co_product = '⊔',
		AAA = '⊓',

		keyboard = '⌨',


		construct = '⫷',
		deconstruct = '⫸',


		name = 0xFFFFFFFFFFFFFFF0,
		space,
		str,
		measure,
		eof,
		comment,
		tree,
	}
	//≣
	//«µ»əɅʌΘ∕❮❯❰❱
	//
	//Ⱶⱻ♪♫↔↕↨∟

	public interface IToken { TokenType type { get; } string src { get; } }

	public class TreeToken : IToken {
		public TokenType type { get; set; } = TokenType.tree;
		public List<IToken> tree;
		public string src => string.Join("", tree.Select(token => token.src));
	}
	public class StrToken : IToken {
		public TokenType type { get; set; }
		public string str;
		public string src { get; set; }
		public string ToString () => $"[{type}] {str}";
	}

	public class Token : IToken {
		public TokenType type { get; set; }
		public string src => $"{(char)type}";
		public string ToString () => $"[{type}] {(char)type}";
	}
	public class MeasureToken : IToken {
		public TokenType type => TokenType.measure;
		public int val;
		public string measure;

		public string src => $"{val}{measure}";
	}
}