grammar Compile;

@members {}
@header {

using System.Collections.Generic;
using System.Linq;


}

parse: block EOF;

block: ( statement | functionDecl)* ('return' expression ';')?;

statement:
	httpRequest ';'
	| varDeclration ';'
	| assignment ';'
	| functionCall ';'
	| forStatement
	| ifStatement;

httpRequest: 'readFile' Identifier # HttpCall;
varDeclration: type Identifier;
assignment: Identifier indexes? '=' expression;

functionCall:
	Identifier '(' exprList? ')'					# identifierFunctionCall
	| Println '(' typespecifier ',' expression? ')'	# printlnFunctionCall;

typespecifier: '%d' | '%s';

functionDecl:
	'func' Identifier '(' idList? ')' '->' type '{' block '}';

forStatement:
	'for' Identifier '=' expression 'to' expression '{' block '}';
ifStatement: ifStat elseIfStat* elseStat?;

ifStat: If expression '{' block '}';

elseIfStat: Else If expression '{' block '}';

elseStat: Else '{' block '}';
idList: type Identifier ( ',' type Identifier)*;

exprList: expression ( ',' expression)*;

expression:
	expression op = ('*' | '/' | '%') expression			# multExpression
	| expression op = ('+' | '-') expression				# addExpression
	| expression op = ('>=' | '<=' | '>' | '<') expression	# compExpression
	| expression op = ('==' | '!=') expression				# eqExpression
	| Number												# numberExpression
	| functionCall indexes?									# functionCallExpression
	| Identifier indexes?									# identifierExpression
	| String indexes?										# stringExpression
	| '(' expression ')' indexes?							# expressionExpression
	| Input '(' String? ')'									# inputExpression;

indexes: ( '[' expression ']')+;

Println: 'println';
type: 'int' | 'string' | 'bool' | 'float';

Input: 'input';
Add: '+';
Subtract: '-';
If: 'if';
Else: 'else';

Bool: 'true' | 'false';

Number: Int ( '.' Digit*)?;

Identifier: [a-zA-Z_] [a-zA-Z_0-9]*;

String:
	["] (~["\r\n\\] | '\\' ~[\r\n])* ["]
	| ['] ( ~['\r\n\\] | '\\' ~[\r\n])* ['];
Comment: ( '//' ~[\r\n]* | '/*' .*? '*/') -> skip;
Space: [ \t\r\n\u000C] -> skip;
fragment Int: [1-9] Digit* | '0';

fragment Digit: [0-9];
