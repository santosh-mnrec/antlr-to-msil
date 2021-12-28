	.assembly extern mscorlib
{
}

	.assembly Program
{
}

.module test.exe

	.class private auto ansi beforefieldinit Program extends [System.Runtime]System.Object 
	{

	.method private hidebysig static void  Main(string[] args) cil managed {
	.entrypoint
	.maxstack  8
	.locals  init (class [System.Net.Http]System.Net.Http.HttpClient client)
	.locals init (class [mscorlib]System.Exception e)
	.try
	{
				ldc.i4  10
		call int32 Program::fib(int32)
		call void [mscorlib]System.Console::Write(int32)

		nop
		leave.s IL_0000
	}
	catch [mscorlib]System.Exception
	{
			stloc.0
			 nop
			ldloc.0
			callvirt instance string [mscorlib]System.Exception::get_Message()
			call void [System.Console]System.Console::WriteLine(string)
			nop
			nop
			leave.s IL_0000
			}
			IL_0000: ret
	}
			.method private hidebysig static int32  fib(int32 a) cil managed{
		.locals init ( int32 a)
		ldarg  a
		ldc.i4  0
		ceq
brfalse  IL_0003
		ldc.i4  0
		ret
		br  IL_0004
IL_0003:
IL_0002:
IL_0004:
		ldarg  a
		ldc.i4  1
		ceq
brfalse  IL_0007
		ldc.i4  1
		ret
		br  IL_0008
IL_0007:
IL_0006:
IL_0008:
		ldarg  a
		ldc.i4  1
		sub
		call int32 Program::fib(int32)
		ldarg  a
		ldc.i4  2
		sub
		call int32 Program::fib(int32)
		add 
		ret
		}


}