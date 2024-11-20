function fib(n) {
    if (n < 2) return 1;
    return fib(n - 1) + fib(n - 2);
}


console.log("Getting 30th fib number...");

var start = Date.now();
console.log(fib(30));
var end = Date.now();
var elapsed = end - start;
console.log("Took " + elapsed + "ms");