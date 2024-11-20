console.log("Starting benchmark of 1 million binary expressions...");

let start = Date.now();
let result = 0;

for (let i = 0; i < 1000000; i++) {
    result = result + (i * 2 - 3) / 2;
}

let end = Date.now();
let elapsed = end - start;

console.log("Result: ", result);
console.log("Milliseconds Elapsed:", elapsed);