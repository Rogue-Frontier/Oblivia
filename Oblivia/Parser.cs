using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Net.WebSockets;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata.Ecma335;
using System.Text.RegularExpressions;
using System.Threading.Tasks.Dataflow;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using static Oblivia.ExMap;
using static System.Formats.Asn1.AsnWriter;
using static System.Runtime.InteropServices.JavaScript.JSType;
namespace Oblivia;
	public class Parser {
		int index;
		List<IToken> tokens;
		public Parser (List<IToken> tokens) {
			this.tokens = tokens;
		}
		public static ExBlock FromFile (string path) {
			var tokenizer = new Lexer(File.ReadAllText(path));
			var tokens = tokenizer.GetAllTokens();
			var src = string.Join("", tokens.Select(t => t.src));
			File.WriteAllText(path, src);
			//tokens.RemoveAll(t => t.type is TokenType.space or TokenType.comment);
			return new Parser(tokens).NextBlock();
		}
		void inc () => index++;
		void dec () => index--;
		public IToken currToken => tokens[index];
		public TokenType currTokenType => currToken.type;
		public string currTokenStr => (currToken as StrToken).str;
	public string currTokenText => (currToken is StrToken st ? st.str : $"{(char)currTokenType}");
		public Node NextStatement () {
			switch(currTokenType) {
				case TokenType.at:
					inc();
					var att = NextTerm();
					return new ExInvokeBlock { type = att, attribute = true, source_block = new ExBlock { statements = [NextStatement()] } };
			}
			var lhs = NextExpr();
			switch(currTokenType) {
				case TokenType.coloneqq:
					inc();
					switch(lhs) {
						case ExUpKey euk:
							return new StAssignSymbol { symbol = euk, value = NextExpr() };
						case ExTuple et: {
								List<ExUpKey> symbols = [];
								foreach(var item in et.vals) {
									if(item is ExUpKey s) {
										symbols.Add(s);
									} else {
										throw new Exception();
									}
								}
								return new StAssignMulti { symbols = symbols.ToArray(), value = NextExpr() };
							}
						case ExBlock eb: {
								List<ExUpKey> symbols = [];
								foreach(var item in eb.statements) {
									if(item is ExUpKey s) {
										symbols.Add(s);
									} else {
										throw new Exception();
									}
								}
								return new StAssignMulti { symbols = symbols.ToArray(), value = NextExpr(), deconstruct = true };
							}
						default: throw new Exception();
					}
				case TokenType.colon:
					inc();
					switch(lhs) {
						case ExVal ev:
						case ExAlias ea:
							//key by value
							throw new Exception("");
						//Local structure define / assign
						case ExBlock eb:
							switch(currTokenType) {
								case TokenType.equal: {
										inc();
										List<ExUpKey> symbols = [];
										foreach(var item in eb.statements) {
											if(item is ExUpKey s) {
												symbols.Add(s);
											} else {
												throw new Exception();
											}
										}
										return new StAssignMulti { symbols = symbols.ToArray(), value = NextExpr(), deconstruct = true };
									}
								default: {
										inc();
										List<string> symbols = [];
										foreach(var item in eb.statements) {
											if(item is ExUpKey s) {
												symbols.Add(s.key);
											} else {
												throw new Exception();
											}
										}
										return new StDefMulti { lhs = symbols.ToArray(), rhs = NextExpr(), deconstruct = true };
									}
							}
						//Local tuple define/assign
						case ExTuple et:
							switch(currTokenType) {
								case TokenType.equal: {
										inc();
										List<ExUpKey> symbols = [];
										foreach(var item in et.vals) {
											if(item is ExUpKey s) {
												symbols.Add(s);
											} else {
												throw new Exception();
											}
										}
										return new StAssignMulti { symbols = symbols.ToArray(), value = NextExpr() };
									}
								default: {
										List<string> symbols = [];
										foreach(var item in et.vals) {
											if(item is ExUpKey { key: { } key, up: -1 or 1 }) {
												symbols.Add(key);
											} else {
												throw new Exception();
											}
										}
										return new StDefMulti { lhs = symbols.ToArray(), rhs = NextExpr() };
									}
							}
						//Local key define/assign
						case ExUpKey es:
							switch(currTokenType) {
								case TokenType.equal:
									inc();
									return new StAssignSymbol { symbol = es, value = NextExpr() };
								default:
									switch(es.up) {
										case -1 or 1:
											return new StDefKey { key = es.key, value = NextExpr() };
										default: throw new Exception("Can only define in current scope");
									}
							}
						//Local method define
						case ExInvoke { target: ExUpKey uk } ei:
							switch(currTokenType) {
								case TokenType.equal:
									throw new Exception("Cannot assign");
								default:
									switch(uk.up) {
										case -1 or 1: return new StDefFn { key = uk.key, pars = ei.args.ParTuple(), value = NextExpr() };
										default: throw new Exception("Cannot define non-local function");
									}
							}

						//Self call define
						case ExInvoke { target: ExSelf { up: 1 or -1 }, args: ExTuple et }:
							return new StDefFn {
								key = "_call",
								pars = et,
								value = NextExpr()
							};
						//Return
						case ExSelf { up: { } up }:
							return new StReturn { val = NextExpr(), up = up };
						case ExFnType eft:
							switch(eft.lhs) {
								//Local key define/assign
								case ExUpKey es:
									switch(currTokenType) {
										case TokenType.equal:
											inc();
											return new StAssignSymbol { symbol = es, value = NextExpr() };
										default:
											switch(es.up) {
												case -1 or 1:
													return new StDefKey { key = es.key, value = NextExpr() };
												default: throw new Exception("Can only define in current scope");
											}
									}
								//Local method define
								case ExInvoke { target: ExUpKey uk } ei:
									switch(uk.up) {
										case -1 or 1: return new StDefFn { key = uk.key, pars = ei.args.ParTuple(), value = NextExpr() };
										default: throw new Exception("Cannot define non-local function");
									}
								//Self call define
								case ExInvoke { target: ExSelf { up: 1 or -1 }, args: ExTuple et }:
									return new StDefFn {
										key = "_call",
										pars = et,
										value = NextExpr()
									};
								//Return
								case ExSelf { up: { } up }:
									return new StReturn { val = NextExpr(), up = up };
							}
							throw new Exception();
						case ExMemberExpr emb:
							throw new Exception();
						case ExMemberBlock emb:
							throw new Exception();
						case ExMap { expr: true, map: ExUpKey euk } em:
							throw new Exception();
						default:
							throw new Exception("Cannot define this");
							/*
							switch(currTokenType) {
                                case TokenType.equal: {
                                        inc();
                                        return new StAssignExpr { lhs = lhs, rhs = NextExpr() };
                                    }
                            }
                            */
					}
			}

			switch(lhs) {
				case ExFnType { lhs: ExUpKey { } k, rhs: { } t } efn:
					return new StDefKey { key = k.key, criteria = t };
				case ExFnType { lhs: ExInvoke { } m, rhs: ExUpKey { } t } efn:
					return new StDefKey { key = (m.target as ExUpKey).key, criteria = new ExFnType { lhs = m.args, rhs = t } };
				case ExFnType efn:
					throw new Exception();
			}
			lhs = CompoundExpr(lhs);
			return lhs;
		}
		public Node NextExpr () {
			var lhs = NextTerm();

			if(index == tokens.Count)
				return lhs;
			return CompoundExpr(lhs);
		}
		public Node CompoundExpr (Node lhs) {
			Node DyadicTerm (ExDyadic.EFn fn) => Dyadic(fn, NextTerm);
			Node DyadicExpr (ExDyadic.EFn fn) => Dyadic(fn, NextExpr);
			Node Dyadic (ExDyadic.EFn fn, Func<Node> n) {
				inc();
				if(currTokenType == TokenType.pipe) {
					return CompoundExpr(new ExDyadicSeq { fn = fn, lhs = lhs, rhs = n() });
				}

				return CompoundExpr(new ExDyadic { fn = fn, lhs = lhs, rhs = n() });
			}
			Start:
			switch(currTokenType) {
				case TokenType.space:
					inc();
					goto Start;
				case TokenType.angle_l:
					inc();
					switch(currTokenType) {
						case TokenType.minus:
							inc();
							return DyadicExpr(ExDyadic.EFn.assign);
						default:
							dec();
							return DyadicTerm(ExDyadic.EFn.lt);
					}
				case TokenType.arrow_e: {
						inc();
						switch(currTokenType) {
							case TokenType.tuple_r:
							case TokenType.block_r:
							case TokenType.array_r:
							case TokenType.angle_r:
								return new ExFnType { lhs = lhs };
							default: {
									var rhs = NextExpr();
									return CompoundExpr(new ExFnType { lhs = lhs, rhs = rhs });
								}
						}
					}
				case TokenType.minus:
					inc();
					switch(currTokenType) {
						//fn type
						case TokenType.angle_r:
							inc();
							switch(currTokenType) {
								case TokenType.tuple_r:
								case TokenType.block_r:
								case TokenType.array_r:
								case TokenType.angle_r:
									return new ExFnType { lhs = lhs };
								default: {
										var rhs = NextExpr();
										return CompoundExpr(new ExFnType { lhs = lhs, rhs = rhs });
									}
							}
						default: {
								dec();
								return DyadicTerm(ExDyadic.EFn.sub);
							}
					}
				case TokenType.block_l:
					return CompoundExpr(new ExInvokeBlock { type = lhs, source_block = NextBlock() });
				case TokenType.pipe: {
						inc();
						switch(currTokenType) {
							case TokenType.star:
								inc();
								return CompoundExpr(new ExMap { src = lhs, map = new ExInvoke { target = new ExSelf { up = 1 }, args = ExTuple.Expr(NextExpr()) }, expr = true });
							case TokenType.period:
								inc();
								return CompoundExpr(new ExMap { src = lhs, map = new ExInvoke { target = new ExSelf { up = 1 }, args = ExTuple.Expr(NextTerm()) }, expr = true });
							case TokenType.slash:
								inc();
								return CompoundExpr(new ExMap { src = lhs, map = NextExpr(), expr = true });
							default: {
									var cond = default(Node);
									var type = default(Node);
									switch(currTokenType) {
										case TokenType.angle_l: {
												inc();
												cond = NextExpr();
												switch(currTokenType) {
													case TokenType.angle_r:
														inc();
														break;
													default:
														throw new Exception("Closing expected");
												}
												break;
											}
									}
									if(currTokenType == TokenType.colon) {
										inc();
										type = NextExpr();
									}
									return CompoundExpr(new ExMap { src = lhs, cond = cond, type = type, map = NextTerm() });
								}
						}
					}
				case TokenType.tuple_l: {
						inc();
						return CompoundExpr(new ExInvoke { target = lhs, args = NextArgTuple() });
					}
				case TokenType.star: {
						inc();
						switch(currTokenType) {
							case TokenType.pipe:
								inc();
								return CompoundExpr(new ExMap {
									src = NextExpr(),
									map = lhs,
								});
							default:
								return CompoundExpr(new ExInvoke {
									target = lhs,
									args = ExTuple.SpreadExpr(NextExpr()),
								});
						}
					}
				case TokenType.period: {
						inc();
						switch(currTokenType) {
							case TokenType.pipe:
								inc();
								return CompoundExpr(new ExMap {
									src = NextTerm(),
									map = lhs,
								});
							case TokenType.period:
								inc();
								return CompoundExpr(new ExTemp { lhs = lhs, rhs = NextExpr() });
							default:
								return CompoundExpr(new ExInvoke { target = lhs, args = ExTuple.SpreadExpr(NextTerm()) });
						}
					}
				case TokenType.bang: {
						inc();
						return CompoundExpr(new ExInvoke { target = lhs, args = ExTuple.Empty });
					}
				case TokenType.equal: {
						inc();
						var Eq = (bool invert) => CompoundExpr(new ExEqual {
							lhs = lhs,
							rhs = NextTerm(),
							invert = invert
						});
						switch(currTokenType) {
							case TokenType.plus: {
									inc();
									return Eq(false);
								}
							case TokenType.minus: {
									inc();
									return Eq(true);
								}
							case TokenType.angle_r: {
									inc();
									var rhs = NextExpr();
								var pars = lhs switch {
									ExTuple et => et,
									ExUpKey euk => new ExTuple { items = [(euk.key, new ExVal { })] }
								};
									return CompoundExpr(new ExFn { pars = pars, result = rhs });
								}
							case TokenType.colon: {
									throw new Exception();
									var rhs = NextTerm();
									//return CompoundExpr(new ExIsAssign { lhs = lhs, rhs = rhs });
								}
							default: {
									var pattern = NextExpr();
									switch(currTokenType) {
										case TokenType.colon:
											inc();
											var symbol = NextSymbol();
											return CompoundExpr(new ExIs { lhs = lhs, rhs = pattern, key = symbol.key });
										default:
											return CompoundExpr(new ExIs { lhs = lhs, rhs = pattern, key = "_" });
									}
									throw new Exception();
								}
						}
						throw new Exception();
					}
				case TokenType.slash: {
						inc();
						switch(currTokenType) {
							case TokenType.pipe:
								inc();
								var fn = NextTerm();
								return CompoundExpr(new ExFn {
									pars = ExTuple.Empty,
									result = new ExInvoke {
										target = fn,
										args = ExTuple.Expr(lhs)
									}
								});
							case TokenType.slash:
								inc();
								//INDEXER
								return CompoundExpr(new ExAt { src = lhs, index = [NextTerm()] });
							case TokenType.star:
								inc();
								return CompoundExpr(new ExAt { src = lhs, index = [NextExpr()] });
							case TokenType.period:
								inc();
								return CompoundExpr(new ExAt { src = lhs, index = [NextTerm()] });
							case TokenType.name:
								var name = currTokenStr;
								inc();
								return CompoundExpr(new ExMemberKey { src = lhs, key = name });
							case TokenType.measure:
								var num = (currToken as MeasureToken).val;
								inc();
								return CompoundExpr(new ExMemberNumber { src = lhs, num = num });
								throw new Exception();
							case TokenType.block_l:
								return CompoundExpr(new ExMemberBlock { lhs = lhs, rhs = (ExBlock)NextExpr(), local = false });
							default:
								return CompoundExpr(new ExMemberExpr { lhs = lhs, rhs = NextExpr(), local = true });
						}
					}
				case TokenType.array_l: {
						var arr = (ExSeq)NextArrayOrLisp();
						return CompoundExpr(new ExAt { src = lhs, index = arr.items });
					}
				/*
			case TokenType.at: {
					inc();
					var term = NextExpr();
					return new ExInvokeBlock { type = term, source_block = new ExBlock { statements = [NextStatement()] } };
					//return CompoundExpr(new ExCompose { items = (ExTuple)NextTupleOrLisp() });
				}
				*/
				case TokenType.question: {
						inc();
						switch(currTokenType) {
							case TokenType.pipe:
								inc();
								var rhs = NextTerm();
								return CompoundExpr(new ExFilter { lhs = lhs, rhs = rhs });
							case TokenType.colon: {
								inc();
								return new ExDyadic { lhs = lhs, rhs = new ExGuardPattern { cond = NextExpr() }, fn = ExDyadic.EFn.intersect };

								throw new Exception();
								//return new ExCriteria { item = lhs, cond = NextExpr() };
							}
							/*
                        case TokenType.tuple_l:
                            inc();
                            var pars = NextArgTuple().ParTuple();
                            switch(currTokenType) {
                                case TokenType.colon:
                                    inc();
                                    break;
                            }
                            var r = new ExFn {
                                pars = pars,
                                result = NextTerm()
                            };
                            return r;
                            */
							case TokenType.tuple_l:
								inc();
								return CompoundExpr(new ExInvoke { target = lhs, args = ExTuple.Expr(NextFn()) });
							case TokenType.array_l: {
									inc();
									Node type = null;
									switch(currTokenType) {
										case TokenType.colon:
											inc();
											type = NextExpr();
											break;
									}
									var items = new List<(Node cond, Node yes, Node no)> { };
									Read:
									var cond = NextExpr();
									switch(currTokenType) {
										case TokenType.colon: {
												inc();
												var yes = NextExpr();
												items.Add((cond, yes, null));
												break;
											}
										default:
											throw new Exception();
									}
									switch(currTokenType) {
										case TokenType.array_r: {
												inc();
												return CompoundExpr(new ExCondSeq {
													type = type,
													filter = lhs,
													items = items
												});
											}
									}
									goto Read;
								}
							case TokenType.block_l: {
									return CompoundExpr(new ExSwitch {
										fn = new ExSwitchFn { branches = NextSwitch() },
										item = lhs
									});
								}
							case TokenType.plus: {
									inc();
									switch(currTokenType) {
										case TokenType.plus:
											inc();
											return CompoundExpr(new ExLoop { condition = lhs, positive = NextExpr() });
											/*
										case TokenType.MINUS:
											inc();
											return CompoundExpr(new ExBranch {
												condition = lhs,
												positive = NextExpr(),
												negative = NextExpr()
											});
											*/
									}
									var positive = NextStatement();
									var negative = default(Node);
									switch(currTokenType) {
										case TokenType.question: {
												inc();
												switch(currTokenType) {
													case TokenType.minus: {
															inc();
															negative = NextStatement();
															break;
														}
													default: {
															dec();
															break;
														}
												}
											}
											break;
									}
									return CompoundExpr(new ExBranch {
										condition = lhs,
										positive = positive,
										negative = negative
									});
								}
							default:
								dec();
								break;
						}
						break;
					}
				case TokenType.cash: {
					inc();
					switch(currTokenType) {
						case TokenType.equal:
							inc();
							return CompoundExpr(new ExDyadic { lhs = lhs, rhs = NextExpr(), fn = ExDyadic.EFn.transform });
							throw new Exception();
					}
					dec();
					return CompoundExpr(new ExDyadic { lhs = lhs, rhs = NextPattern(), fn = ExDyadic.EFn.intersect });
				}
				case TokenType.ellipsis:	return DyadicTerm(ExDyadic.EFn.range);
				case TokenType.neq:			return DyadicTerm(ExDyadic.EFn.neq);
				case TokenType.and:			return DyadicTerm(ExDyadic.EFn.and);
				case TokenType.or:			return DyadicTerm(ExDyadic.EFn.or);
				case TokenType.xor:			return DyadicTerm(ExDyadic.EFn.xor);
				case TokenType.nand:		return DyadicTerm(ExDyadic.EFn.nand);
				case TokenType.nor:			return DyadicTerm(ExDyadic.EFn.nor);
				case TokenType.plus:		return DyadicTerm(ExDyadic.EFn.add);
				//case TokenType.minus: return DyadicTerm(ExDyadic.EFn.sub);
				case TokenType.times:		return DyadicTerm(ExDyadic.EFn.mul);
				case TokenType.divide:		return DyadicTerm(ExDyadic.EFn.div);
				case TokenType.gt:			return DyadicTerm(ExDyadic.EFn.gt);
				//case TokenType.lt:    return DyadicTerm(ExDyadic.EFn.lt);
				case TokenType.geq:			return DyadicTerm(ExDyadic.EFn.geq);
				case TokenType.leq:			return DyadicTerm(ExDyadic.EFn.leq);
				case TokenType.ceil:		return DyadicTerm(ExDyadic.EFn.max);
				case TokenType.floor:		return DyadicTerm(ExDyadic.EFn.min);
				case TokenType.exists:		return DyadicTerm(ExDyadic.EFn.exists);
				case TokenType.not_exists:	return DyadicTerm(ExDyadic.EFn.not_exists);
				case TokenType.for_all:		return DyadicTerm(ExDyadic.EFn.for_all);
				//case TokenType.double_plus:             return DyadicTerm(ExDyadic.EFn.concat);
				case TokenType.count:		return DyadicTerm(ExDyadic.EFn.count);
				case TokenType.log:			return DyadicTerm(ExDyadic.EFn.log);
				case TokenType.range:		return DyadicTerm(ExDyadic.EFn.range);
				case TokenType.square_fill_l: return DyadicTerm(ExDyadic.EFn.take);
				case TokenType.square_fill_r: return DyadicTerm(ExDyadic.EFn.drop);
				case TokenType.deal:		return DyadicTerm(ExDyadic.EFn.deal);
				case TokenType.arrow_w:		return DyadicExpr(ExDyadic.EFn.assign);
				case TokenType.first: inc(); return CompoundExpr(new ExMonadic { rhs = lhs, fn = ExMonadic.EFn.first });
				case TokenType.last: inc(); return CompoundExpr(new ExMonadic { rhs = lhs, fn = ExMonadic.EFn.last });
				case TokenType.construct: return DyadicTerm(ExDyadic.EFn.construct);
				case TokenType.compose: return DyadicExpr(ExDyadic.EFn.compose);
			}
			return lhs;
		}
		Node NextTerm () {
			Read:
			switch(currTokenType) {
				case TokenType.empty: return Val(VEmpty.VALUE);
				case TokenType.yes: return Val(true);
				case TokenType.no: return Val(false);
				case TokenType.not: return MonadicTerm(ExMonadic.EFn.not);
				case TokenType.floor: return MonadicTerm(ExMonadic.EFn.floor);
				case TokenType.ceil: return MonadicTerm(ExMonadic.EFn.ceil);
				case TokenType.range: return MonadicTerm(ExMonadic.EFn.range);
				case TokenType.count: return MonadicTerm(ExMonadic.EFn.count);
				case TokenType.log: return MonadicTerm(ExMonadic.EFn.log);
				case TokenType.index_descend: return MonadicTerm(ExMonadic.EFn.index_descend);
				case TokenType.index_ascend: return MonadicTerm(ExMonadic.EFn.index_ascend);
				case TokenType.dice: return MonadicTerm(ExMonadic.EFn.dice);
				case TokenType.keyboard: return MonadicTerm(ExMonadic.EFn.keyboard);
				/*
			case TokenType.minus:{
				inc();
					switch(tokenType) {
						//fn type
						case TokenType.angle_r: {
								inc();
								switch(tokenType) {
									case TokenType.tuple_r:
									case TokenType.block_r:
									case TokenType.array_r:
									case TokenType.angle_r:
										return new ExFnType{};
									default:
										var output = NextExpr();
										return (new ExFnType {rhs = output});
								}
							}
						case TokenType.tuple_r:
						case TokenType.block_r:
						case TokenType.array_r:
							return new ExRange { };
						default:
							var to = NextExpr();
							return (new ExRange { rhs = to });
					}
				}
				*/
				case TokenType.name: return NextSymbol();
				case TokenType.caret: return NextCaretSymbol();
				case TokenType.str: return NextString();
				case TokenType.measure: return NextInteger();
				case TokenType.question: return NextQuestion();
				case TokenType.amp: return NextRef();
				case TokenType.square_brack_l: return NextLisp();
				case TokenType.tuple_l: return NextTupleOrLisp();
				case TokenType.array_l: return NextArrayOrLisp();
				case TokenType.block_l: return NextBlock();
				case TokenType.percent:
					inc();
					return new ExSpread { value = NextExpr() };
				case TokenType.quote: return NextAlias();
				case TokenType.cash: return NextPattern();
				case TokenType.comma:
					inc();
					goto Read;
				case TokenType.space:
					inc();
					goto Read;
				case TokenType.comment:
					inc();
					goto Read;
			}
			throw new Exception($"Unexpected token {currToken.type} in expression at index {index}");
			ExVal Val (object val) {
				inc();
				return new ExVal { value = val };
			}
			ExMonadic MonadicTerm (ExMonadic.EFn fn) {
				inc();
				return new ExMonadic { fn = fn, rhs = NextTerm() };
			}
		}
		List<(Node cond, Node yes)> NextSwitch () {
			inc();
			var items = new List<(Node cond, Node yes)> { };
			ReadBranch:
			switch(currTokenType) {
				case TokenType.block_r:
					inc();
					return items;
			}
			//To match multiple conds, use any()
			var cond = NextExpr();
			switch(currTokenType) {
				case TokenType.colon:
					inc();
					var branch = NextExpr();
					items.Add((cond, branch));
					goto ReadBranch;
			}
			switch(cond) {
				case ExFn fn:
					items.Add((fn.pars, fn.result));
					goto ReadBranch;
				default:
					throw new Exception("Unknown branch format");
			}
		}
		ExRef NextRef () {
			inc();
			return new ExRef { expr = NextExpr() };
		}
		ExAlias NextAlias () {
			inc();
			return new ExAlias { expr = NextExpr() };
		}
	public Node NextPattern () {
		inc();
		switch(currTokenType) {
			case TokenType.pipe: {
					inc();
					return new ExMonadic { rhs = NextTerm(), fn = ExMonadic.EFn.sat };
				}
			case TokenType.colon: {
					inc();
					return new ExGuardPattern { cond = NextExpr() };
				}
			case TokenType.slash: {

					//
					//	Valid examples
					//	$/abc abc/
					//  $/abc:.+ abc/
					return new ExVal {
						value = ReadRegex()
					};
					PatternString ReadRegex(){
						List<string> seq = new();
						inc();
						Read:
						switch(currTokenType) {
							case TokenType.name: {
									var key = currTokenStr;
									inc();
									if(currTokenType == TokenType.colon) {
										inc();
										var pattern = "";
										Read2:
										if(currTokenType == TokenType.space) {
											seq.Add(PatternString.MakePattern(pattern, key));
											inc();
											goto Read;
										}
										if(currTokenType == TokenType.slash) {
											seq.Add(PatternString.MakePattern(pattern, key));
											goto Read;
										}
										if(currTokenType == TokenType.cash) {
											inc();
											if(currTokenType == TokenType.slash) {
												var rr = ReadRegex();
												pattern += rr.RegexPattern;
												goto Read2;
											}
											dec();
										}
										pattern += currTokenText;
										inc();
										goto Read2;
									} else {
										seq.Add(PatternString.MakePattern(key));
									}
									goto Read;
								}
							case TokenType.slash: {
									inc();
									var res = string.Join("", seq);
									return new PatternString { pattern = res };
								}
							case TokenType.space: inc(); goto Read;
							default:
								seq.Add(PatternString.MakePattern(currTokenText));
								inc();
								goto Read;
						}
					}
				}
		}
				var expr = NextTerm();
				switch(expr) {
					case ExVal { value: string { } str } ev: {
							return new ExVal { value = new Regex(str) };
						}
					case ExVal { value: int i }: {
							return new ExVal { value = i };
						}
					case ExTuple et:
						var binds = et.items.Select(p => (p.key, (Node?)p.value)).ToList();
						return new ExTuplePattern {
							rest = true,
							binds = binds
						};
					case ExSeq es:
						throw new Exception();
					case ExBlock eb:
						return new ExStructurePattern {
							rest = true,
							binds = eb.statements.Select(p => p switch {
								ExIs ei => ((ei.lhs as ExUpKey).key, ei.rhs, ei.key),
								StDefKey sdk => (sdk.key, sdk.value, sdk.key),
							}).ToList()
						};
					case ExUpKey euk: {
							return new ExWildcardPattern { key = euk.key };
						}
					/*
					foreach(var st in eb.statements) {
						switch(st) {
							case StDefKey sdk: {
									var lhs = sdk.key;
									var key = sdk.key;
									var rhs = sdk.criteria;
									throw new Exception();
								}
							case ExIs eis: {
									var lhs = eis.lhs;
									var key = eis.key;
									var rhs = eis.rhs;
									throw new Exception();
								}
						}
					}
					*/

					case ExInvokeBlock eib:
					default:
						throw new Exception();
				}
		}
		string ReadLispOp () {
			var start = index;
			var d = new Dictionary<string, int> {
				["+"] = 1,
				["-"] = 2,
				["*"] = 3,
				["/"] = 4,
				["&"] = 5,
				["|"] = 6,
				["^"] = 7,
				["%"] = 8,
				["="] = 9,
				[">"] = 10,
				["<"] = 11,
				[">="] = 12,
				["<="] = 13,
				[">>"] = 14,
				["<<"] = 15,
				["&&"] = 16,
				["||"] = 17,
				["++"] = 18,
				["--"] = 19,
				["**"] = 20,
				["//"] = 21,
			};
			var op = "";
			ReadOp:
			switch(currTokenType) {
				case TokenType.plus:
				case TokenType.minus:
				case TokenType.star:
				case TokenType.slash:
				case TokenType.caret:
				case TokenType.pipe:
				case TokenType.amp:
				case TokenType.percent:
				case TokenType.equal:
				case TokenType.angle_l:
				case TokenType.angle_r:
				case TokenType.period:
					op += currToken.src;
					inc();
					goto ReadOp;
				case TokenType.colon:
					if(op == "")
						return "";
					inc();
					return op;
				default:
					index = start;
					return "";
			}
		}
		Node NextLisp () {
			inc();
			var items = new List<Node>();
			Read:
			switch(currTokenType) {
				case TokenType.square_brack_r:
					inc();
					throw new Exception();
				default:
					items.Add(NextExpr());
					goto Read;
			}
		}
		Node NextTupleOrLisp () {
			inc();
			var op = ReadLispOp();
			//handle empty tuple
			switch(currTokenType) {
				case TokenType.tuple_r:
					inc();
					return new ExVal { value = VEmpty.VALUE };
			}
			var expr = NextExpr();
			switch(currTokenType) {
				case TokenType.tuple_r: {
						inc();
						return expr;
					}
				default:
					var t = NextTuple(expr);
					if(op.Any()) {
						return new ExLisp { args = [.. t.vals], op = op };
					}
					return t;
			}
		}
		ExTuple NextArgTuple () {
			switch(currTokenType) {
				case TokenType.tuple_r:
					inc();
					return ExTuple.Empty;
				default:
					return NextTuple(NextExpr());
			}
		}
		ExTuple NextTuple (Node first) {
			var items = new List<(string key, Node val)> { };
			return AddEntry(first);
			ExTuple AddEntry (Node lhs) {
				switch(currTokenType) {
					case TokenType.colon:
						NextPair(lhs);
						break;
					default:
						items.Add((null, lhs));
						break;
				}
				switch(currTokenType) {
					case TokenType.tuple_r:
						inc();
						return new ExTuple { items = items.ToArray() };
					default:
						return AddEntry(NextExpr());
				}
			}
			void NextPair (Node lhs) {
				switch(lhs) {
					case ExUpKey { up: -1, key: { } key }: {
							inc();
							var val = NextExpr();
							items.Add((key, val));
							break;
						}
					default:
						throw new Exception("Name expected");
				}
			}
		}
		Node NextArrayOrLisp () {
			List<Node> items = [];
			inc();
			Node type = null;
			switch(currTokenType) {
				case TokenType.colon:
					inc();
					type = NextExpr();
					break;
			}
			Check:
			switch(currTokenType) {
				case TokenType.comma:
					inc();
					goto Check;
				case TokenType.array_r:
					inc();
					return new ExSeq { items = items, type = type };
				default:
					var item = NextExpr();
					if(currTokenType == TokenType.colon) {

					} else {

					}
					switch(currTokenType) {
						case TokenType.colon:
							var l = new List<Node> { item };
							while(currTokenType == TokenType.colon) {
								inc();
								l.Add(NextExpr());
							}
							var tuple = ExTuple.ListExpr(l);
							items.Add(tuple);
							break;
						default:
							items.Add(item);
							break;
					}
					/*
                    switch(tokenType) {
                        case TokenType.COLON:
                            type = item;
                            inc();
                            goto Check;
                    }
                    */
					goto Check;
			}
		}
		Node NextQuestion () {
			inc();
			var t = currTokenType;
			switch(t) {
				case TokenType.block_l:
					var branches = NextSwitch();
					return new ExSwitchFn { branches = branches };
				case TokenType.array_l:
					throw new Exception();
				case TokenType.tuple_l:
					inc();
					return NextFn();
				case TokenType.str:
					var str = currTokenStr;
					inc();
					List<Node> parts = [];
					var text = "";
					var i = 0;
					Read:
					if(i < str.Length) {
						switch(str[i]) {
							case '{':
								parts.Add(new ExVal { value = text });
								i++;
								text = "";
								ReadChar:
								if(i < str.Length && str[i] is char _c and not '}') {
									text += _c;
									i++;
									goto ReadChar;
								} else {
									i++;
								}
								parts.Add(new Parser(new Lexer(text).GetAllTokens()).NextExpr());
								text = "";
								goto Read;
							case char c:
								text += c;
								i++;
								goto Read;

						}
					} else {
						parts.Add(new ExVal { value = text });
					}
					return new ExInterpolate { parts = parts };
				default:
					throw new Exception($"Unexpected token {t}");
			}
		}
		Node NextFn () {
			var pars = NextArgTuple().ParTuple();
			object retType = null;
			Check:
			switch(currTokenType) {
				case TokenType.arrow_e: {
						inc();
						retType = NextExpr();
						goto Check;
						//?(a:i4) -> i4:{}
					}
				case TokenType.array_l:
				case TokenType.tuple_l:
				case TokenType.block_l: {
						return new ExFn {

							//retType = retType,
							pars = pars,
							result = NextTerm()
						};
					}
				case TokenType.period:
					inc();
					return new ExFn {
						//retType = retType,
						pars = pars,
						result = NextTerm()
					};
				case TokenType.star:
				case TokenType.colon:
					inc();
					return new ExFn {

						//retType = retType,
						pars = pars,
						result = NextExpr()
					};
				default:
					//We do not accept floating form
					throw new Exception("deprecated");
			}
		}
		ExUpKey NextSymbol () {
			var name = currTokenStr;
			inc();
			return new ExUpKey { key = name, up = -1 };
		}
		Node NextCaretSymbol () {
			inc();
			int up = 1;
			Check:
			switch(currTokenType) {
				case TokenType.caret: {
						up += 1;
						inc();
						goto Check;
					}
				case TokenType.name: {
						var s = new ExUpKey { up = up, key = currTokenStr };
						inc();
						return s;
					}
				/*
			case TokenType.cash: {
					//Return This
					var s = new ExSelf { up = up };
					inc();
					return s;
				}
				*/
				/*
			case TokenType.L_PAREN:
				return new ExMemberTuple { lhs = new ExSelf { up = up }, rhs = (ExTuple)NextExpression(), local = true };
				*/
				default:
					return new ExSelf { up = up };
			}
		}
		public ExVal NextString () {
			var value = currTokenStr;
			inc();
			return new ExVal { value = value };
		}
		public ExVal NextInteger () {
			var value = (currToken as MeasureToken).val;
			inc();
			return new ExVal { value = value };
		}
		public ExBlock NextBlock () {
			inc();
			var ele = new List<Node>();
			Check:
			switch(currTokenType) {
				case TokenType.pipe:
					throw new Exception();
				case TokenType.block_r:
					inc();
					return new ExBlock { statements = ele };
				case TokenType.comma:
					inc();
					goto Check;
				case TokenType.space:
				case TokenType.comment:
					inc();
					goto Check;
				default:
					ele.Add(NextStatement());
					goto Check;
			}
			throw new Exception($"Unexpected token in object expression: {currTokenType}");
		}
	}
/*
point ?{
    $[0 0]:print("origin")
    $[$_ 0]:print()
    $[0 $_]:print()
    $[$x $y] ?:(y = x):print()
    $[$x $y] ?:(y = 0-x):print()
    $[$x $y]: print()
}


*/
    public interface LVal {
        object Assign (IScope ctx, Func<object> getVal);
        //IEnumerable<LVal> Unpack ();
    }