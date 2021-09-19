grammar Compile;

@members {}
@header { 

using System.Collections.Generic;
using System.Linq;


}

parse: block EOF;

block: ( statement | functionDecl)* ('return' expression ';')?;

statement:
	readFile ';'
	| varDeclaration ';'
	| assignment ';'
	| functionCall ';'  
	| forStatement
	| ifStatement;   

readFile: 'readFile' Identifier # ReadFileCall;
varDeclaration: type Identifier;
assignment: Identifier '=' expression;

functionCall:
	Identifier '(' exprList? ')'					# identifierFunctionCall
	| Println '(' dataType ',' expression? ')'	# printlnFunctionCall
	| Print '(' dataType ',' expression? ')'	# printFunctionCall;



dataType: '%d' | '%s' |'%f';

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
	| functionCall											# functionCallExpression
	| Identifier											# identifierExpression
	| String												# stringExpression
	| '(' expression ')'									# expressionExpression
	| Input '(' String? ')'									# inputExpression;



Println: 'println';
Print: 'print';
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
