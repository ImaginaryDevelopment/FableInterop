module FableInterop

open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.Browser

[<Emit("undefined")>]
let undefined : obj = jsNative
console.log(undefined)
[<Emit("1")>]
let one : int = jsNative
let result = one + one
console.log(result)

[<Emit("$0 === undefined")>]
// calling this isUndefined1, because I have a different preference for the real isUndefined
let isUndefined1 (x: 'a) : bool = jsNative

// null is not undefined in my version (which is great for differentiating between passing null, or forgetting to pass a value, or some other things with objects I think)
[<Emit("!($0 != null)")>]
let isUndefined (x:'A) : bool = jsNative

// if we don't specify 'a is obj then this doesn't compile
let logUndefinedTests (f:obj -> bool) =
    // javascript special cases of concern: "", 0, false, null
    console.log(f 5)
    console.log(f "")
    console.log(f [||])
    console.log(f false)
    console.log(f 0)
    // this one is a difference with mine
    console.log(f null)
logUndefinedTests isUndefined1
logUndefinedTests isUndefined




console.log("Fable is up and running...")

