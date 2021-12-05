func fact(int n)->int{

    int f;
    f=1;
    for i=1 to n {

        f=f*i;
    }
    return f;
    }


int result;
result=fact(5);

print(%d,result);

