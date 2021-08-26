// Root non-terminal symbol A program is a bunch of declarations followed by a bunch of statements
// The Java code outputs the necessary NASM code around these declarations

grammar Compile;

@members {}
@header {

using System.Collections.Generic;
using System.Linq;


}

parse: block EOF;

block: ( statement | functionDecl)* ('return' expression)?;

statement: assignment ';' | functionCall ';' | forStatement;

assignment: Identifier indexes? '=' expression;

functionCall:
	Identifier '(' exprList? ')'	# identifierFunctionCall
	| Println '(' expression? ')'	# printlnFunctionCall;

functionDecl: 'func' Identifier '(' idList? ')' '{' block '}';

forStatement:
	'for' Identifier '=' expression 'to' expression '{' block '}';

idList: Identifier ( ',' Identifier)*;

exprList: expression ( ',' expression)*;

expression:
	 expression op = ('*' | '/' | '%') expression	#multExpression
	| expression op = ('+' | '-') expression		#addExpression
	| Number										#numberExpression
	| functionCall indexes?							#functionCallExpression
	| Identifier indexes?							#identifierExpression
	| String indexes?								#stringExpression
	| '(' expression ')' indexes?					#expressionExpression
	| Input '(' String? ')'							#inputExpression;

indexes: ( '[' expression ']')+;

Println: 'println';

Input: 'input';
Add: '+';
Subtract: '-';


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
