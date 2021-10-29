
int c;
c=100;
func fib(int a,int b)->int{
   c=10;
   return a+b+c;
}
int c;

c=fib(1,2);
println(%d,c);


//factorial
func factorial(int a)->int{

   if(a==1){
      return 1;
   }
   else{
      return a*factorial(a-1);
   }
}
int f;
f=factorial(5);
println(%d,f);