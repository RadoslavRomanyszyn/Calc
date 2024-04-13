using System;
using System.Linq;
using System.Collections.Generic;

namespace Calc
{
    enum TokenType
    {
        INTEGER, VAR, PLUS, MINUS, MUL, DIV, LPAREN, RPAREN, POW, EOF
    }

    class Token
    {
        public TokenType type;
        public object value;

        public Token(TokenType type, object value)
        {
            this.type = type;
            this.value = value;
        }
    }

    class Tokenizer
    {
        string text;
        int pos;
        char currentChar;

        public Tokenizer(string text)
        {
            this.text = text;
            pos = 0;
            currentChar = this.text[pos];
        }

        public class InvalidCharacterException : Exception
        {

        }

        void Error()
        {
            throw new InvalidCharacterException();
        }

        void Advance()
        {
            pos++;
            if (pos > text.Length - 1)
                currentChar = '&';
            else
                currentChar = text[pos];
        }

        void SkipWhitespace()
        {
            while (currentChar != '&' && char.IsWhiteSpace(currentChar))
                Advance();
        }

        int Integer()
        {
            string result = "";
            while (currentChar != '&' && char.IsDigit(currentChar))
            {
                result += currentChar;
                Advance();
            }
            return Convert.ToInt32(result);
        }

        string Var()
        {
            string name = "";
            while (currentChar != '&' && char.IsLetter(currentChar))
            {
                name += currentChar;
                Advance();
            }
            return name;
        }

        public Token GetNextToken()
        {
            while (currentChar != '&')
            {
                if (char.IsWhiteSpace(currentChar))
                {
                    SkipWhitespace();
                    continue;
                }

                if (char.IsDigit(currentChar))
                    return new Token(TokenType.INTEGER, Integer());

                if (char.IsLetter(currentChar))
                    return new Token(TokenType.VAR, Var());

                switch (currentChar)
                {
                    case '+':
                        Advance();
                        return new Token(TokenType.PLUS, '+');
                    case '-':
                        Advance();
                        return new Token(TokenType.MINUS, '-');
                    case '*':
                        Advance();
                        return new Token(TokenType.MUL, '*');
                    case '/':
                        Advance();
                        return new Token(TokenType.DIV, '/');
                    case '(':
                        Advance();
                        return new Token(TokenType.LPAREN, '(');
                    case ')':
                        Advance();
                        return new Token(TokenType.RPAREN, ')');
                    case '^':
                        Advance();
                        return new Token(TokenType.POW, '^');
                }
                Error();
            }
            return new Token(TokenType.EOF, null);
        }
    }

    class AST
    {

    }

    class UnaryOp : AST
    {
        public Token token;
        public AST expr;

        public UnaryOp(Token token, AST expr)
        {
            this.token = token;
            this.expr = expr;
        }
    }

    class BinOp : AST
    {
        public AST left;
        public Token token;
        public AST right;

        public BinOp(AST left, Token token, AST right)
        {
            this.left = left;
            this.token = token;
            this.right = right;
        }
    }

    class Num : AST
    {
        public int value;

        public Num(int value)
        {
            this.value = value;
        }
    }

    class Var : AST
    {
        public string name;
        public int coef;
        public int pow;

        public Var(string name)
        {
            this.name = name;
            coef = 1;
            pow = 1;
        }
    }

    class Parser
    {
        Tokenizer tokenizer;
        Token currentToken = new Token(TokenType.EOF, null);

        public Parser(Tokenizer tokenizer)
        {
            this.tokenizer = tokenizer;
            try
            {
                currentToken = this.tokenizer.GetNextToken();
            }
            catch (Tokenizer.InvalidCharacterException)
            {
                Console.WriteLine("Exception: Invalid Character");
            }
        }

        public class InvalidSyntaxException : Exception
        {

        }

        void Error()
        {
            throw new InvalidSyntaxException();
        }

        void Check(TokenType type)
        {
            if (currentToken.type == type)
                try
                {
                    Token lastToken = currentToken;
                    currentToken = tokenizer.GetNextToken();

                    if (lastToken.type == TokenType.INTEGER && currentToken.type == TokenType.INTEGER
                        || lastToken.type == TokenType.VAR && currentToken.type == TokenType.VAR)
                        Error();
                    if (lastToken.type == TokenType.INTEGER && currentToken.type == TokenType.VAR
                        || lastToken.type == TokenType.VAR && currentToken.type == TokenType.INTEGER)
                        Error();
                    if (lastToken.type == TokenType.INTEGER && currentToken.type == TokenType.LPAREN
                        || lastToken.type == TokenType.VAR && currentToken.type == TokenType.LPAREN)
                        Error();
                    if (lastToken.type == TokenType.RPAREN && currentToken.type == TokenType.INTEGER
                        || lastToken.type == TokenType.RPAREN && currentToken.type == TokenType.VAR)
                        Error();

                }
                catch (Tokenizer.InvalidCharacterException)
                {
                    Console.WriteLine("Exception: Invalid Character");
                }
            else
                Error();
        }

        AST Atom()
        {
            Token token = currentToken;

            if (token.type == TokenType.PLUS)
            {
                Check(TokenType.PLUS);
                return new UnaryOp(token, Factor());
            }
            else if (token.type == TokenType.MINUS)
            {
                Check(TokenType.MINUS);
                return new UnaryOp(token, Factor());
            }
            else if (token.type == TokenType.INTEGER)
            {
                Check(TokenType.INTEGER);
                return new Num((int)token.value);
            }
            else if (token.type == TokenType.VAR)
            {
                Check(TokenType.VAR);
                return new Var((string)token.value);
            }
            else if (token.type == TokenType.LPAREN)
            {
                Check(TokenType.LPAREN);
                AST node = Expr();
                Check(TokenType.RPAREN);
                return node;
            }
            Error();
            return new AST();
        }

        AST Factor()
        {
            AST node = Atom();

            while (currentToken.type == TokenType.POW)
            {
                Token token = currentToken;

                Check(TokenType.POW);

                node = new BinOp(node, token, Factor());
            }
            return node;
        }

        AST Term()
        {
            AST node = Factor();

            while (currentToken.type == TokenType.MUL || currentToken.type == TokenType.DIV)
            {
                Token token = currentToken;

                if (token.type == TokenType.MUL)
                    Check(TokenType.MUL);
                else if (token.type == TokenType.DIV)
                    Check(TokenType.DIV);

                node = new BinOp(node, token, Factor());
            }
            return node;
        }

        AST Expr()
        {
            AST node = Term();

            while (currentToken.type == TokenType.PLUS || currentToken.type == TokenType.MINUS)
            {
                Token token = currentToken;

                if (token.type == TokenType.PLUS)
                    Check(TokenType.PLUS);
                else if (token.type == TokenType.MINUS)
                    Check(TokenType.MINUS);

                node = new BinOp(node, token, Term());
            }
            return node;
        }

        public AST Parse()
        {
            return Expr();
        }
    }

    class Evaluator
    {
        Parser parser;

        public Evaluator(Parser parser)
        {
            this.parser = parser;
        }

        public class NothingToSimplifyException : Exception
        {

        }

        void Error()
        {
            throw new NothingToSimplifyException();
        }

        public class ZeroRaisedToZeroException : Exception
        {

        }

        AST Simplify(AST tree)
        {
            if (tree is UnaryOp && ((UnaryOp)tree).token.type == TokenType.PLUS)
            {
                AST simp = Simplify(((UnaryOp)tree).expr);

                if (simp is Num || simp is Var)
                    return simp;
                if (simp is BinOp)
                {
                    if (((BinOp)simp).token.type == TokenType.PLUS || ((BinOp)simp).token.type == TokenType.MINUS)
                        return simp;
                }
                return new UnaryOp(((UnaryOp)tree).token, simp);
            }

            else if (tree is UnaryOp unaryOp && unaryOp.token.type == TokenType.MINUS)
            {
                AST simp = Simplify(unaryOp.expr);

                if (simp is Num num)
                {
                    num.value = -num.value;
                    return num;
                }
                if (simp is Var var)
                {
                    var.coef = -var.coef;
                    return var;
                }
                if (simp is BinOp binOp)
                {
                    if (binOp.token.type == TokenType.PLUS || binOp.token.type == TokenType.MINUS)
                        return new BinOp(Simplify(new UnaryOp(new Token(TokenType.MINUS, '-'), binOp.left)),
                            binOp.token,
                            Simplify(new UnaryOp(new Token(TokenType.MINUS, '-'), binOp.right)));
                }
                return new UnaryOp(unaryOp.token, simp);
            }



            else if (tree is Num || tree is Var)
                return tree;



            else if (tree is BinOp binOp)
            {
                if (binOp.token.type == TokenType.PLUS || binOp.token.type == TokenType.MINUS)
                {
                    if (binOp.left is Num && binOp.right is Num)
                    {
                        if (binOp.token.type == TokenType.PLUS)
                            return new Num(((Num)binOp.left).value + ((Num)binOp.right).value);
                        else
                            return new Num(((Num)binOp.left).value - ((Num)binOp.right).value);
                    }
                    if (binOp.left is Var && binOp.right is Var)
                    {
                        if (((Var)binOp.left).name == ((Var)binOp.right).name && ((Var)binOp.left).pow == ((Var)binOp.right).pow)
                        {
                            Var var = new Var(((Var)binOp.left).name);
                            var.pow = ((Var)binOp.left).pow;
                            if (binOp.token.type == TokenType.PLUS)
                            {
                                var.coef = ((Var)binOp.left).coef + ((Var)binOp.right).coef;
                            }
                            else
                            {
                                var.coef = ((Var)binOp.left).coef - ((Var)binOp.right).coef;
                            }
                            return var;
                        }
                    }
                       
                    return new BinOp(Simplify(binOp.left),
                                              binOp.token,
                                     Simplify(binOp.right));
                }

                

                else if (binOp.token.type == TokenType.POW)
                {
                    /*if (binOp.left is Num)
                    {
                        return new Num((int)Math.Pow(((Num)binOp.left).value, ((Num)binOp.right).value));
                    }
                    else if (binOp.left is Var)
                    {
                        Var var = new Var(((Var)binOp.left).name);
                        var.pow = ((Var)binOp.left).pow * ((Num)binOp.right).value;
                        var.coef = ((Var)binOp.left).coef;
                        return var;
                    }
                    else
                        return Simplify(new BinOp(Simplify(binOp.left), 
                                         new Token(TokenType.POW, '^'), 
                                         Simplify(binOp.right)));*/
                    int pow = 0;

                    try
                    {
                        pow = ((Num)Simplify(binOp.right)).value;
                    }
                    catch
                    {
                        Error();
                    }

                    if (pow == 0)
                    {
                        AST radix;
                        try
                        {
                            radix = Simplify(binOp.left);
                        }
                        catch (NothingToSimplifyException)
                        {
                            return new Num(1);
                        }

                        if (radix is Num && ((Num)radix).value == 0)
                            throw new ZeroRaisedToZeroException();
                        if (radix is Var && ((Var)radix).coef == 0)
                            throw new ZeroRaisedToZeroException();

                        return new Num(1);
                    }
                    if (pow == 1)
                    {
                        if (binOp.left is Num num)
                            return num;
                        if (binOp.left is Var var)
                            return var;
                        if (binOp.left is UnaryOp unary)
                            return Simplify(unary);
                        if (binOp.left is BinOp binary)
                            return Simplify(binary);
                    }
                    if (pow > 1)
                        return Simplify(new BinOp(binOp.left, 
                                   new Token(TokenType.MUL, '*'), 
                                   new BinOp(binOp.left, new Token(TokenType.POW, '*'), new Num(pow - 1))));
                }




                else if (binOp.token.type == TokenType.MUL)
                {
                    if (binOp.left is UnaryOp || binOp.right is UnaryOp)
                        return Simplify(new BinOp(Simplify(binOp.left),
                                             binOp.token,
                                             Simplify(binOp.right)));
                    else if (binOp.left is BinOp left)
                    {
                        if (left.token.type == TokenType.PLUS || left.token.type == TokenType.MINUS)
                            return new BinOp(Simplify(new BinOp(left.left, new Token(TokenType.MUL, '*'), binOp.right)),
                                             left.token,
                                             Simplify(new BinOp(left.right, new Token(TokenType.MUL, '*'), binOp.right)));
                        else if (left.token.type == TokenType.MUL || left.token.type == TokenType.DIV)
                            return Simplify(new BinOp(Simplify(left), new Token(TokenType.MUL, '*'), Simplify(binOp.right)));
                        else if (left.token.type == TokenType.POW)
                            return Simplify(new BinOp(Simplify(left), new Token(TokenType.MUL, '*'), Simplify(binOp.right)));
                    }
                    else if (binOp.right is BinOp right)
                    {
                        if (right.token.type == TokenType.PLUS || right.token.type == TokenType.MINUS)
                            return new BinOp(Simplify(new BinOp(right.left, new Token(TokenType.MUL, '*'), binOp.left)),
                                             right.token,
                                             Simplify(new BinOp(right.right, new Token(TokenType.MUL, '*'), binOp.left)));
                        else if (right.token.type == TokenType.MUL || right.token.type == TokenType.DIV)
                            return Simplify(new BinOp(Simplify(right), new Token(TokenType.MUL, '*'), Simplify(binOp.left)));
                        else if (right.token.type == TokenType.POW)
                            return Simplify(new BinOp(Simplify(right), new Token(TokenType.MUL, '*'), Simplify(binOp.left)));
                    }

                    else if (binOp.left is Var && binOp.right is Num)
                    {
                        Var var = new Var(((Var)binOp.left).name);
                        var.coef = ((Var)binOp.left).coef * ((Num)binOp.right).value;
                        var.pow = ((Var)binOp.left).pow;
                        return var;
                    }

                    else if (binOp.left is Num && binOp.right is Var)
                    {

                        Var var1 = new Var(((Var)binOp.right).name);
                        var1.coef = ((Var)binOp.right).coef * ((Num)binOp.left).value;
                        var1.pow = ((Var)binOp.right).pow;
                        return var1;
                    }

                    else if (binOp.left is Num && binOp.right is Num)
                    {
                        Num num = new Num(((Num)binOp.left).value * ((Num)binOp.right).value);
                        return num;
                    }

                    else if (binOp.left is Var && binOp.right is Var)
                    {
                        if (((Var)binOp.left).name == ((Var)binOp.right).name)
                        {
                            Var var2 = new Var(((Var)binOp.left).name);
                            var2.coef = ((Var)binOp.left).coef * ((Var)binOp.right).coef;
                            var2.pow = ((Var)binOp.left).pow + ((Var)binOp.right).pow;
                            return var2;
                        }
                    }

                }




                else if (binOp.token.type == TokenType.DIV)
                {
                    if (binOp.left is UnaryOp || binOp.right is UnaryOp)
                        return Simplify(new BinOp(Simplify(binOp.left),
                                             binOp.token,
                                             Simplify(binOp.right)));
                    else if (binOp.left is BinOp left)
                    {
                        if (left.token.type == TokenType.PLUS || left.token.type == TokenType.MINUS)
                        {
                            if (!((left.left is Num && left.right is Var) || (left.left is Var && left.right is Num)))
                                return Simplify(new BinOp(Simplify(left), new Token(TokenType.DIV, '/'), Simplify(binOp.right)));
                        }
                        else if (left.token.type == TokenType.MUL || left.token.type == TokenType.DIV)
                            return Simplify(new BinOp(Simplify(left), new Token(TokenType.DIV, '/'), Simplify(binOp.right)));
                        else if (left.token.type == TokenType.POW)
                            return Simplify(new BinOp(Simplify(left), new Token(TokenType.DIV, '/'), Simplify(binOp.right)));
                    }
                    else if (binOp.right is BinOp right)
                    {
                        if (right.token.type == TokenType.PLUS || right.token.type == TokenType.MINUS)
                        {
                            if (!((right.left is Num && right.right is Var) || (right.left is Var && right.right is Num)))
                                return Simplify(new BinOp(Simplify(binOp.left), new Token(TokenType.DIV, '/'), Simplify(right)));
                        }
                        else if (right.token.type == TokenType.MUL || right.token.type == TokenType.DIV)
                            return Simplify(new BinOp(Simplify(binOp.left), new Token(TokenType.DIV, '/'), Simplify(right)));
                        else if (right.token.type == TokenType.POW)
                            return Simplify(new BinOp(Simplify(binOp.left), new Token(TokenType.DIV, '/'), Simplify(right)));
                    }

                    else if (binOp.left is Var && binOp.right is Num)
                    {
                        Var var = new Var(((Var)binOp.left).name);
                        var.coef = ((Var)binOp.left).coef / ((Num)binOp.right).value;
                        var.pow = ((Var)binOp.left).pow;
                        return var;
                    }

                    else if (binOp.left is Num && binOp.right is Var)
                    {

                        Var var1 = new Var(((Var)binOp.right).name);
                        var1.coef = ((Num)binOp.left).value / ((Var)binOp.right).coef;
                        var1.pow = -((Var)binOp.right).pow;
                        return var1;
                    }

                    else if (binOp.left is Num && binOp.right is Num)
                    {
                        Num num = new Num(((Num)binOp.left).value / ((Num)binOp.right).value);
                        return num;
                    }

                    else if (binOp.left is Var && binOp.right is Var)
                    {
                        if (((Var)binOp.left).name == ((Var)binOp.right).name)
                        {
                            Var var2 = new Var(((Var)binOp.left).name);
                            var2.coef = ((Var)binOp.left).coef / ((Var)binOp.right).coef;
                            var2.pow = ((Var)binOp.left).pow - ((Var)binOp.right).pow;
                            return var2;
                        }
                    }
                    else if (binOp.left.Equals(binOp.right))
                        return new Num(1);

                }

            }
            Error();
            return new AST();
        }


        List<AST> Plus(AST tree)
        {
            if (tree is Num)
                return new List<AST> { tree };
            if (tree is Var)
                return new List<AST> { tree };
            if (tree is BinOp binOp)
            {
                if (binOp.token.type == TokenType.PLUS)
                    return Plus(binOp.left).Join(Plus(binOp.right));
                if (binOp.token.type == TokenType.MINUS)
                    return Plus(binOp.left).Join(Minus(binOp.right));
            }
            return new List<AST>();
        }

        List<AST> Minus(AST tree)
        {
            if (tree is Num num)
            {
                num.value = -num.value;
                return new List<AST> { num };
            }
            if (tree is Var var)
            {
                var.coef = -var.coef;
                return new List<AST> { var };
            }
            if (tree is BinOp binOp)
            {
                if (binOp.token.type == TokenType.PLUS)
                    return Minus(binOp.left).Join(Minus(binOp.right));
                if (binOp.token.type == TokenType.MINUS)
                    return Minus(binOp.left).Join(Plus(binOp.right));
            }
            return new List<AST>();
        }


        public class VarComparer : IComparer<(string, int)>
        { 
            public int Compare((string, int) x, (string, int) y)
            {
                if (x.Item1 == y.Item1)
                    return -(x.Item2.CompareTo(y.Item2));
                return x.CompareTo(y);
            }
        
        }



        string Stringify(AST tree)
        {
            int cnst = 0;
            VarComparer varComparer = new VarComparer();
            SortedDictionary<(string, int), int> varCoef = new SortedDictionary<(string, int), int>(varComparer);

            List<AST> nodesToSum = Plus(tree);

            foreach (AST node in nodesToSum)
            {
                if (node is Num num)
                    cnst += num.value;
                else if (node is Var var)
                {
                    try
                    {
                        varCoef.Add((var.name, var.pow), var.coef);
                    }
                    catch (ArgumentException)
                    {
                        varCoef[(var.name, var.pow)] += var.coef;
                    }
                }
            }

            string result = "";
            /*foreach (KeyValuePair<(string, int), int> var in varCoef)
            {
                result += var;
                result += ' ';
            }
            result += cnst;*/


            foreach (KeyValuePair<(string, int), int> var in varCoef)
            {
                if (var.Key.Item2 == 0)
                    cnst += var.Value;
                else if (var.Value == 0)
                    continue;
                else if (var.Value == 1 && result != "")
                {
                    if (var.Key.Item2 == 1)
                        result += "+ " + var.Key.Item1 + " ";
                    else if (var.Key.Item2 != 1)
                        result += "+ " + var.Key.Item1 + "^" + var.Key.Item2 + " ";
                }
                else if (var.Value == 1 && result == "")
                {
                    if (var.Key.Item2 == 1)
                        result += var.Key.Item1 + " ";
                    else if (var.Key.Item2 != 1)
                        result += var.Key.Item1 + "^" + var.Key.Item2 + " ";
                }
                else if (var.Value == -1 && result != "")
                {
                    if (var.Key.Item2 == 1)
                        result += "- " + var.Key.Item1 + " ";
                    else if (var.Key.Item2 != 1)
                        result += "- " + var.Key.Item1 + "^" + var.Key.Item2 + " ";
                }
                else if (var.Value == -1 && result == "")
                {
                    if (var.Key.Item2 == 1)
                        result += "-" + var.Key.Item1 + " ";
                    else if (var.Key.Item2 != 1)
                        result += "-" + var.Key.Item1 + "^" + var.Key.Item2 + " ";
                }
                else if (var.Value > 1 && result != "")
                {
                    if (var.Key.Item2 == 1)
                        result += "+ " + var.Value + "*" + var.Key.Item1 + " ";
                    else if (var.Key.Item2 != 1)
                        result += "+ " + var.Value + "*" + var.Key.Item1 + "^" + var.Key.Item2 + " ";
                }
                else if (var.Value > 1 && result == "")
                {
                    if (var.Key.Item2 == 1)
                        result += var.Value + "*" + var.Key.Item1 + " ";
                    else if (var.Key.Item2 != 1)
                        result += var.Value + "*" + var.Key.Item1 + "^" + var.Key.Item2 + " ";
                }
                else if (var.Value < -1 && result != "")
                {
                    if (var.Key.Item2 == 1)
                        result += "- " + Math.Abs(var.Value) + "*" + var.Key.Item1 + " ";
                    else if (var.Key.Item2 != 1)
                        result += "- " + Math.Abs(var.Value) + "*" + var.Key.Item1 + "^" + var.Key.Item2 + " ";
                }
                else if (var.Value < -1 && result == "")
                {
                    if (var.Key.Item2 == 1)
                        result += var.Value + "*" + var.Key.Item1 + " ";
                    else if (var.Key.Item2 != 1)
                        result += var.Value + "*" + var.Key.Item1 + "^" + var.Key.Item2 + " ";
                }
            }
            if (result == "")
                result += cnst;
            else
            {
                if (cnst > 0)
                    result += "+ " + cnst;
                if (cnst < 0)
                    result += "- " + Math.Abs(cnst);
            }



            return result;
        }

        public string Evaluate()
        {
            AST tree = parser.Parse();
            AST simplified_tree = Simplify(tree);
            return Stringify(simplified_tree);
        }
    }

    public static class Extension
    {
        public static List<T> Join<T>(this List<T> first, List<T> second)
        {
            if (first == null)
            {
                return second;
            }
            if (second == null)
            {
                return first;
            }

            return first.Concat(second).ToList();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                string expression = "";
                Console.Write(">>> ");
                try
                {
                    expression = Console.ReadLine();
                }
                catch
                {
                    break;
                }
                if (expression == "")
                    continue;
                

                Tokenizer tokenizer = new Tokenizer(expression);
                Parser parser = new Parser(tokenizer);
                Evaluator evaluator = new Evaluator(parser);

                try
                {
                    Console.WriteLine(evaluator.Evaluate());
                }
                catch (DivideByZeroException)
                {
                    Console.WriteLine("Exception: Division by Zero Not Defined");
                    continue;
                }
                catch (Tokenizer.InvalidCharacterException)
                {
                    Console.WriteLine("Exception: Invalid Character");
                    continue;
                }
                catch (Parser.InvalidSyntaxException)
                {
                    Console.WriteLine("Exception: Invalid Syntax");
                    continue;
                }
                catch (Evaluator.NothingToSimplifyException)
                {
                    Console.WriteLine(expression);
                    continue;
                }
                catch (Evaluator.ZeroRaisedToZeroException)
                {
                    Console.WriteLine("Exception: Zero Raised to Zero Not Defined");
                    continue;
                }
            }
        }
    }
}
