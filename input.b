

string s;
s="Hello world";
print(%s,s);

func fib(int a)->int {
	if(a<=2){
	return 1;
	}
	else{
	return fib(a-1)+fib(a-2);
	
	}
}

int c;
c=fib(5);
print(%d,c);