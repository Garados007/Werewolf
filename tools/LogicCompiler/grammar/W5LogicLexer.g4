lexer grammar W5LogicLexer;

KW_ABSTRACT: 'abstract';
KW_MODE: 'mode';
KW_CHARACTER: 'character';
KW_WIN: 'win';
KW_PHASE: 'phase';
KW_SCENE: 'scene';
KW_LABEL: 'label';
KW_VOTING: 'voting';
KW_OPTION: 'option';
KW_SEQUENCE: 'sequence';
KW_EVENT: 'event';
KW_STRING: 'string';
KW_INT: 'int';
KW_BOOL: 'bool';

KW_AFTER: 'after';
KW_BEFORE: 'before';
KW_FUNC: 'func';
KW_TARGET: 'target';
KW_STEP: 'step';

KW_SPAWN: 'spawn';
KW_NOTIFY: 'notify';
KW_WITH: 'with';
KW_IF: 'if';
KW_ELSE: 'else';
KW_LET: 'let';
KW_FOR: 'for';

COND_IF: '#if';
COND_ELSE: '#else';
COND_END: '#end';

BO: '(';
BC: ')';
CBO: '{';
CBC: '}';
COMMA: ',';
COLON: ':';
SEMICOLON: ';';
OP_OR: '||';
PIPE: '|';
OP_EQUAL: '==';
ASSIGN: '=';
OP_GE: '>=';
OP_LE: '<=';
OP_GT: '>';
OP_LT: '<';
OP_UNEQUAL: '!=';

OP_AND: '&&';
OP_ADD: '+';
OP_SUB: '-';
OP_MUL: '*';
OP_DIV: '/';
OP_ASTERIX: '!';

GLOBAL_COL: '@' [a-zA-Z_][a-zA-Z_0-9]*;
VARIABLE: '$' [a-zA-Z_][a-zA-Z_0-9]*;
ID: [a-zA-Z_][a-zA-Z_0-9]*;

STRING:
	'"' (
		~[\r\n"\\]
		| '\\' ['"\\0abefnrtv]
		| '\\u' HEX HEX HEX HEX
		| '\\U00' HEX HEX HEX HEX HEX HEX
		| '\\x' HEX (HEX (HEX HEX?)?)?
	)* '"';
INT: [0-9]+;
COMMENT: '//' (~[\r\n])* -> skip;
WS: [ \t\n\r\f]+ -> skip;

fragment HEX: [a-fA-F0-9];
