const FIB_NUM = 20;

function fib(n) {
    if (n < 2) return 1;
    return fib(n - 1) + fib(n - 2);
}


console.log("Getting " + FIB_NUM + "th fib number...");

var start = Date.now();
console.log(fib(FIB_NUM));
var end = Date.now();
var elapsed = end - start;
console.log("Took " + elapsed + "ms");