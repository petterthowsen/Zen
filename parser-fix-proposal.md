# Parser Fix Proposal for Operator Precedence

## Current Issue

The expression `print 50 + 10 * 2 - 5` is incorrectly parsed as:

```
Program
  PrintStmt
    Binary
      Literal Int, token: IntLiteral(`50`)
      Plus(`+`)
      Binary
        Literal Int, token: IntLiteral(`10`)
        Star(`*`)
        Literal Int, token: IntLiteral(`2`)
  ExpressionStmt
    Unary
      Minus(`-`)
      Literal Int, token: IntLiteral(`5`)
```

When it should be:

```
Program
  PrintStmt
    Binary
      Binary
        Literal Int, token: IntLiteral(`50`)
        Plus(`+`)
        Binary
          Literal Int, token: IntLiteral(`10`)
          Star(`*`)
          Literal Int, token: IntLiteral(`2`)
      Minus(`-`)
      Literal Int, token: IntLiteral(`5`)
```

## Root Cause

The current parser's expression parsing methods don't properly handle operator precedence. The key issues are:

1. The Term and Factor methods are processing operators in reverse order
2. The binary expression construction doesn't properly chain operations
3. Whitespace handling between operators may be affecting parsing

## Proposed Fix

### 1. Correct Expression Parsing Hierarchy

```csharp
private Expr Expression() {
    return Assignment();
}

private Expr Assignment() {
    Expr expr = Addition(); // Changed from Equality()

    MaybeSome(TokenType.Whitespace);

    if (Match(TokenType.Assign, TokenType.PlusAssign, TokenType.MinusAssign, TokenType.StarAssign, TokenType.SlashAssign)) {
        Token op = Previous;
        MaybeSome(TokenType.Whitespace);
        Expr value = Assignment();

        if (expr is Identifier identifier) {
            return new Assignment(op, identifier, value);
        }
        Error("Invalid assignment target.", ErrorType.RuntimeError);
    }

    return expr;
}

// New method for addition/subtraction
private Expr Addition() {
    Expr expr = Multiplication();

    while (true) {
        MaybeSome(TokenType.Whitespace);
        
        if (Match(TokenType.Plus)) {
            Token op = Previous;
            MaybeSome(TokenType.Whitespace);
            Expr right = Multiplication();
            expr = new Binary(expr, op, right);
        }
        else if (Match(TokenType.Minus)) {
            Token op = Previous;
            MaybeSome(TokenType.Whitespace);
            Expr right = Multiplication();
            expr = new Binary(expr, op, right);
        }
        else {
            break;
        }
    }

    return expr;
}

// New method for multiplication/division
private Expr Multiplication() {
    Expr expr = Equality();

    while (true) {
        MaybeSome(TokenType.Whitespace);
        
        if (Match(TokenType.Star)) {
            Token op = Previous;
            MaybeSome(TokenType.Whitespace);
            Expr right = Equality();
            expr = new Binary(expr, op, right);
        }
        else if (Match(TokenType.Slash)) {
            Token op = Previous;
            MaybeSome(TokenType.Whitespace);
            Expr right = Equality();
            expr = new Binary(expr, op, right);
        }
        else {
            break;
        }
    }

    return expr;
}

private Expr Equality() {
    Expr expr = Comparison();

    while (true) {
        MaybeSome(TokenType.Whitespace);
        
        if (Match(TokenType.Equal, TokenType.NotEqual)) {
            Token op = Previous;
            MaybeSome(TokenType.Whitespace);
            Expr right = Comparison();
            expr = new Binary(expr, op, right);
        }
        else {
            break;
        }
    }

    return expr;
}

private Expr Comparison() {
    Expr expr = Unary();

    while (true) {
        MaybeSome(TokenType.Whitespace);
        
        if (Match(TokenType.LessThan, TokenType.LessThanOrEqual,
                 TokenType.GreaterThan, TokenType.GreaterThanOrEqual)) {
            Token op = Previous;
            MaybeSome(TokenType.Whitespace);
            Expr right = Unary();
            expr = new Binary(expr, op, right);
        }
        else {
            break;
        }
    }

    return expr;
}
```

### 2. Key Changes

1. Introduced separate methods for Addition and Multiplication to properly handle operator precedence
2. Changed the expression parsing hierarchy to:
   - Expression → Assignment
   - Assignment → Addition
   - Addition → Multiplication
   - Multiplication → Equality
   - Equality → Comparison
   - Comparison → Unary
   - Unary → Primary

3. Consistent whitespace handling between all operators
4. Proper left-to-right evaluation of operators at the same precedence level

### 3. Benefits

1. Correctly handles operator precedence (* and / before + and -)
2. Properly chains multiple operations
3. Maintains correct associativity for operators
4. Preserves whitespace handling

### 4. Example Parse Result

For the expression `print 50 + 10 * 2 - 5`, the parser will now produce:

```
Program
  PrintStmt
    Binary
      Binary
        Literal Int, token: IntLiteral(`50`)
        Plus(`+`)
        Binary
          Literal Int, token: IntLiteral(`10`)
          Star(`*`)
          Literal Int, token: IntLiteral(`2`)
      Minus(`-`)
      Literal Int, token: IntLiteral(`5`)
```

This correctly represents the mathematical expression `(50 + (10 * 2)) - 5`, respecting operator precedence.
