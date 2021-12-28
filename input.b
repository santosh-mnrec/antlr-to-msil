

func fib(int a)->int{
    if(a==0){
        return 0;
    }
    if(a==1){
        return 1;
    }
    return fib(a-1)+fib(a-2);
}

print(%d,fib(10));