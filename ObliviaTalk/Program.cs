using Oblivia;
var parser = Parser.FromFile("ObliviaTalk.obl");
var result = (VDictScope)parser.StagedEval(Std.std);
var r = (result.locals["main"] as VFn).CallVoid(result);
