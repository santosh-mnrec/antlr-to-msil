func fib(int n) ->int{
    if (n == 0) {
        return 0;
    }
    else if (n == 1) {
        return 1;
    }
    else{
    return fib(n-1) + fib(n-2);
    }
}

int result;
result = fib(10);
print(%d, result);