module FableInterop

open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.Browser

// following https://medium.com/@zaid.naom/f-interop-with-javascript-in-fable-the-complete-guide-ccc5b896a59f
console.log("Fable is up and running...")

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
    console.group("logUndefinedTests")
    // javascript special cases of concern: "", 0, false, null
    console.log(f 5)
    console.log(f "")
    console.log(f [||])
    console.log(f false)
    console.log(f 0)
    // this one is a difference with mine
    console.log(f null)
    console.groupEnd()
logUndefinedTests isUndefined1
logUndefinedTests isUndefined

[<Emit("$0 + $1")>]
let add (x:int) (y:int) = jsNative
// console.log isn't exactly reflective of console.log
// this doesn't work the way I expect:
// console.log("add", add 5 10)

console.log(add 5 10)

[<Emit("isNaN($0)")>]
let isNaN (x:'a) = jsNative
console.log(log -2.0)
console.log(isNaN (log -2.0))

[<Emit("Math.random()")>]
let getRandom() : float = jsNative
console.log(getRandom())
// returns int not float (also note: supported)
console.log(System.Random().Next())

module BadParseFloat =
    [<Emit("isNaN(parseFloat($0)) ? null : parseFloat($0)  ")>]
    let parseFloat' (input : string) : float option = jsNative
    let directNativeParseFloat() =
        console.group("directNativeParseFloat")

        // Correct parsing
        match parseFloat' "5.3" with
        | Some value -> console.log(value)
        | None -> console.log("parseFloat failed!")

        // Parsing fails
        match parseFloat' "%" with
        | Some value -> console.log(value)
        | None -> console.log("parseFloat failed!")

        match parseFloat' "5x" with
        | Some value -> console.log(value)
        | None -> console.log("parseFloat failed!")
        console.groupEnd()


BadParseFloat.directNativeParseFloat()

[<Emit("isNaN(+$0) ? null : (+$0)")>]
let parseFloat (input : string) : float option = jsNative

match parseFloat "5x" with
| Some result -> console.log(result)       //  Parsing fails as it should
| None -> console.log("No result found")   //  logs "No result found"