parser grammar W5LogicParser;
options {
	tokenVocab = W5LogicLexer;
}

program: nodes += nodeBase* EOF;

nodeBase: isAbstract = KW_ABSTRACT? spec = nodeSpec;

nodeSpec:
	node = modeDef			# ModeNode
	| node = phaseDef		# PhaseNode
	| node = sceneDef		# SceneNode
	| node = labelDef		# LabelNode
	| node = characterDef	# CharacterNode
	| node = votingDef		# VotingNode
	| node = optionDef		# OptionNode
	| node = winDef			# WinNode
	| node = sequenceDef	# SequenceNode
	| node = eventDef		# EventNode;

inherit: COLON ids += ID (COMMA ids += ID)*;

modeDef:
	KW_MODE name = ID base = inherit? CBO (
		KW_CHARACTER characters += idList
		| KW_WIN wins += idList
	)* CBC;

phaseDef:
	KW_PHASE name = ID base = inherit? CBO (funcs += func)* CBC;

sceneDef:
	KW_SCENE name = ID base = inherit? CBO (
		KW_PHASE phases += ID SEMICOLON
		| cycles += cycleDef
		| funcs += func
	)* CBC;

labelDef:
	KW_LABEL name = ID base = inherit? CBO (
		KW_TARGET CBO targets += (
			KW_MODE
			| KW_CHARACTER
			| KW_PHASE
			| KW_SCENE
			| KW_VOTING
		) (
			COMMA targets += (
				KW_MODE
				| KW_CHARACTER
				| KW_PHASE
				| KW_SCENE
				| KW_VOTING
			)
		)* CBC
		| withs += with
		| funcs += func
	)* CBC;

characterDef:
	KW_CHARACTER name = ID base = inherit? CBO (funcs += func)* CBC;

votingDef:
	KW_VOTING name = ID base = inherit? CBO (
		KW_TARGET targets += ID SEMICOLON
		| funcs += func
	)* CBC;

optionDef: KW_OPTION name = ID SEMICOLON;

winDef:
	KW_WIN name = ID base = inherit? CBO (funcs += func)* CBC;

sequenceDef:
	KW_SEQUENCE name = ID base = inherit? CBO (steps += step)* CBC;

eventDef:
	KW_EVENT name = ID base = inherit? CBO (
		targets += eventTarget
		| funcs += func
	)* CBC;

eventTarget:
	KW_TARGET modifier = (KW_PHASE | KW_SCENE)? name = ID CBO (
		steps += step
	)* CBC;

cycleDef: cycle = (KW_AFTER | KW_BEFORE) ids = idList;

with:
	KW_WITH type = (KW_CHARACTER | KW_STRING | KW_INT | KW_BOOL) name = ID SEMICOLON;

func: KW_FUNC name = ID code = codeBlock;

step: KW_STEP name = ID code = codeBlock;

idList: CBO (names += ID (COMMA names += ID)*)? CBC;

//codeBlock: CBO ((~(CBC|CBO)) | codeBlock)* CBC;
codeBlock: CBO stmts = statementList? CBC;

statementList: stmts += statement (stmts += statement)*;

statement:
	expr = expression sem = SEMICOLON? # StmtExpression
	| KW_SPAWN (
		voting = KW_VOTING name = ID (
			KW_WITH withs += ID BO expr += expression BC
		)*
		| sequence = KW_SEQUENCE name = ID BO expr += expression BC
		| event = KW_EVENT OP_MUL
	) SEMICOLON															# StmtSpawn
	| KW_NOTIFY KW_CHARACTER name = ID BO exp = expression BC SEMICOLON	# StmtNotifyPlayer
	| KW_NOTIFY (KW_SEQUENCE sequence = ID)? name = (
		ID
		| STRING
		| VARIABLE
	) SEMICOLON # StmtNotify
	| COND_IF expr = expression success = statementList? (
		COND_ELSE fail = statementList?
	)? COND_END													# StmtCondIf
	| KW_LET name = VARIABLE ASSIGN expr = expression SEMICOLON	# StmtLet;

argument: (name = ID ASSIGN)? expr = expression;

expression:
	left = expression (PIPE right += pipeCall)+ # ExprPipe
	| left = expression op = (
		OP_EQUAL
		| OP_UNEQUAL
		| OP_GE
		| OP_LE
		| OP_GT
		| OP_LT
	) right = expression											# ExprComp
	| left = expression op = (OP_OR | OP_AND) right = expression	# ExprOrAnd
	| left = expression op = (OP_ADD | OP_SUB) right = expression	# ExprAddSub
	| left = expression op = (OP_MUL | OP_DIV) right = expression	# ExprMulDiv
	| OP_ASTERIX value = expression									# ExprNegate
	| CBO (expr += expression (COMMA expr += expression)*)? CBC		# ExprGroup
	| KW_IF KW_LET name = VARIABLE ASSIGN expr = expression success = codeBlock (
		KW_ELSE fail = codeBlock
	)? # ExprIfLet
	| KW_IF expr = expression success = codeBlock (
		KW_ELSE fail = codeBlock
	)?																			# ExprIf
	| KW_FOR KW_LET name = VARIABLE ASSIGN expr = expression loop = codeBlock	# ExprForLet
	| name = GLOBAL_COL															# ExprGlobal
	| name = VARIABLE															# ExprVariable
	| name = ID (
		open = BO (args += argument (COMMA args += argument)*)? BC
	)?					# ExprCall
	| value = STRING	# ExprString
	| value = INT		# ExprInt
	| type = (
		KW_MODE
		| KW_CHARACTER
		| KW_WIN
		| KW_PHASE
		| KW_SCENE
		| KW_VOTING
		| KW_SEQUENCE
		| KW_EVENT
		| KW_OPTION
		| KW_LABEL
	) name = ID # ExprType;

pipeCall:
	name = ID BO (expr += expression (COMMA expr += expression)*)? BC;
