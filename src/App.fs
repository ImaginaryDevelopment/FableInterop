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

module ParseFloatInefficient =

    [<Emit("isNaN(+$0) ? null : (+$0)")>]
    let parseFloat (input : string) : float option = jsNative

    match parseFloat "5x" with
    | Some result -> console.log(result)       //  Parsing fails as it should
    | None -> console.log("No result found")   //  logs "No result found"

module ParseFloat =
    [<Emit("(x => isNaN(x) ? null : x)(+$0)")>]
    let parseFloat (input : string) : float option = jsNative
    module DoubleTryParse =
        let systemFloatParse x =
            console.group "double.TryParse"
            let (isSuccess,value) as result = System.Double.TryParse x
            match result with
            | true, x -> console.log(x)
            | false, _ -> console.log("double.TryParse failed")
            console.groupEnd()
            // as long as I return something besides unit, the rest of this method runs in the bizzarre section. really strange
            value

        // if systemFloatParse returns unit this acts up, and doesn't run the method
        let bizarreOrUnexpectedMaybeBugs =
            console.group "buggy?"
            // this acts strangely, the systemFloatParse function doesn't appear to be evaluated, instead it just prints an empty object {}
            systemFloatParse "1.2" // returns unit (but doesn't evaluate the function!
            // x is unit, so it logging an empty object {} makes sense
            |> fun x -> console.log(x)
            // proof that I'm not returning
            |> ignore<obj>
            // this does the same
            console.log(systemFloatParse "1.2")
            console.groupEnd()

        // this does what I would expect
        systemFloatParse "1.2"
        |> ignore<obj>
    module DoubleParse =
        let systemFloatParse x =
            console.group "double.Parse"
            console.log(System.Double.Parse x)
        systemFloatParse "1.2"
