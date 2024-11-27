using Oblivia;
var parser = Parser.FromFile("ObliviaTalk.obl");
var result = (ValDictScope)parser.StagedEval(Std.std);
var r = (result.locals["main"] as ValFunc).CallVoid(result);
