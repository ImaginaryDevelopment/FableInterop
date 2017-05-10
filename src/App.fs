[<System.Diagnostics.CodeAnalysis.SuppressMessage("NameConventions","MemberNamesMustBePascalCase")>]
module FableInterop

open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.Browser

// following https://medium.com/@zaid.naom/f-interop-with-javascript-in-fable-the-complete-guide-ccc5b896a59f

// console.clear here, means any output from previous javascript to load, would be hidden/lost
// console.clear()
console.log("Fable is up and running...")

[<Emit("undefined")>]
let undefined : obj = jsNative
type Console with
    [<Emit("console.log($0,$1)")>]
    member __.log2 a b = jsNative
    // let log2 a b = jsNative

// works but not available anywhere else, as this seems to be nearly the last thing to run
// so nice to see in dead comments, but not to keep in or use
// let inspect (x:obj) (title:string) :obj =
//     console.log2 title x
//     x


[<Emit("window[$0] = $1")>]
let defineGlobal (name:string) (x:'A) : unit = jsNative
console.log(undefined)

[<Emit("1")>]
let one : int = jsNative
let result = one + one
console.log(result)

[<Emit("$0 === undefined")>]
// calling this isUndefined1, because I have a different preference for the real isUndefined
let isUndefined (x: 'a) : bool = jsNative

[<Emit("$0 != null")>]
let isDefined (x: 'a) : bool = jsNative

// null is not undefined in my version (which is great for differentiating between passing null, or forgetting to pass a value, or some other things with objects I think)
// [<Emit("!($0 != null)")>]
// let isUndefined (x:'A) : bool = jsNative


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
    [<Emit("isNaN(parseFloat($0)) ? null : parseFloat($0)")>]
    let parseFloat' (input : string) : float option = jsNative
    let directNativeParseFloat() =
        console.group("directNativeParseFloat")

        // Correct parsing
        match parseFloat' "5.3" with
        | Some value123 -> console.log(value123)
        | None -> console.log("parseFloat failed!")

        // Parsing fails
        match parseFloat' "%" with
        | Some value -> console.log(value)
        | None -> console.log("parseFloat failed!")

        match parseFloat' "5x" with
        | Some value -> console.log(value)
        | None -> console.log("parseFloat failed!")
        console.groupEnd()
        if false then
            raise <| System.NotImplementedException("bad directnativeparsefloat")


BadParseFloat.directNativeParseFloat()

module ParseFloatInefficient =

    [<Emit("isNaN(+$0) ? null : (+$0)")>]
    let parseFloat (input : string) : float option = jsNative

    match parseFloat "5x" with
    | Some result -> console.log(result)       //  Parsing fails as it should
    | None -> console.log("No result found")   //  logs "No result found"

module ParseFloat =
    // other implementations can evaluate $0 twice, if they were complex functions, they would run twice
    // create a function to hold the result of +input and check it for isNaN, return null if so, otherwise the number value
    [<Emit("(x => isNaN(x) ? null : x)(+$0)")>]
    let parseFloat (input : string) : float option = jsNative
    parseFloat("5x")
    |> fun x -> console.log(x)

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
            console.groupEnd()
        systemFloatParse "1.2"

[<System.Diagnostics.CodeAnalysis.SuppressMessage("NameConventions","MemberNamesMustBePascalCase")>]
module JQueryMap =
    // [<System.Diagnostics.CodeAnalysis.SuppressMessage("NameConventions","MemberNamesMustBePascalCase")>]
    type IJQuery =
        [<System.Diagnostics.CodeAnalysis.SuppressMessage("NameConventions","MemberNamesMustBePascalCase")>]
        abstract css : string * string -> IJQuery
        abstract addClass : string -> IJQuery
        // for awhile not using .on for event attaching was listed as deprecated in jQuery (or a bad practice, I don't remember which)
        // there's also an overload that actually triggers a click
        [<Emit("$0.click($1)")>]
        abstract click : (obj -> unit) -> IJQuery
        [<Emit("$0.click()")>]
        abstract click : unit -> IJQuery
        [<Emit("$0.on('click', $1)")>]
        abstract onClick : (obj -> unit) -> IJQuery

    // moving these methods into a JQuery module did not change the resulting output so says [WDS] in the console
    module JQuery =
        [<Emit("window['$']($0)")>]
        let select (selector:string) : IJQuery = jsNative

        [<Emit("window['$']($0)")>]
        let ready (handler: unit -> unit) : unit = jsNative

        [<Emit("$2.css($0, $1)")>]
        let css (prop: string) (value: string) (el: IJQuery) : IJQuery = jsNative

        // I don't see any difference in the resulting code in this being here, vs it not being here (except anotherSample2)
        // |> fun x -> x.addClass results in ugly code either way
        // |> addClass "fancy3" seems to result in pretty code
        [<Emit("$1\r\n.addClass($0)")>]
        let addClass (className: string) (el: IJQuery) : IJQuery = jsNative

        // \r\n in the emit doesn't appear to help the output =(
        [<Emit("$1\r\n.click($0)")>]
        let click (handler: obj -> unit)  (el: IJQuery) : IJQuery = jsNative

    // methodChainingSample
    let sample () =
        JQuery.select("#main")
            .addClass("fancy")
            .click(fun ev -> console.log("clicked"))
            .css("background-color", "red")
            .click()
        |> ignore
    sample()

    //shadowing sequence sample
    let anotherSample () =
        console.group("anotherSample")
        let main = JQuery.select("#main")
        console.log(main)
        let main = main.addClass("fancy2")
        let main = main.onClick(fun ev -> console.log("clicked2"); console.log(ev))
        // this emit attempt doesn't compile =(
        // [<Emit("'$'main")>]
        // sadly this maps to _main not $main like it would in javascript
        let ``$main`` = main.css ("background-color", "red")
        let main = ``$main``.click()

        console.groupEnd()
    anotherSample()

    // piping sequence sample
    let anotherSample2() =
        console.group "anotherSample5"
        JQuery.select "#main"
        // this produces horribly ugly code for some reason
        // |> fun x -> x.addClass "fancy3"
        |> JQuery.addClass "fancy3"
        |> JQuery.click (fun ev -> console.log("clicked3"))
        // piping still works? are the .css calls above using what the emit says to use?
        |> JQuery.css "background-color" "blue"
        |> ignore<IJQuery>
        console.groupEnd()
    anotherSample2()
    // dynamic programming (discouraged)
    module JQueryDynamic =
        module JQuery =
            [<Emit("window['$']($0)")>]
            let select (selector: string) = jsNative
        let div: obj = JQuery.select "#main"

        // fade isn't defined in jQuery, perhaps this code was meant for jQueryUI?
        !!div?css("any","prop")?html("non-empty") //?fade(400)
        |> ignore



    console.log("jQuery stuff done!")

// it seems this produces multiple lines of editing a fresh {}, not a single {current:..., amount:..., unit:...}
module ObjectLiterals =
    open System
    [<System.Diagnostics.CodeAnalysis.SuppressMessage("NameConventions", "InterfaceNamesMustBeginWithI")>]
    type AddTimeProps =
        abstract current : DateTime with get,set
        [<Emit("$0.specialAmount{{=$1}}")>]
        abstract amount : int with get,set
        abstract unit : string with get,set


    let parameter = createEmpty<AddTimeProps>
    parameter.current <- DateTime.Now
    parameter.amount <- 20
    parameter.unit <- "days"

[<StringEnum>]
type TimeUnit =
    | Days
    | Months
    | [<CompiledName("YEARS")>] Years
console.log(TimeUnit.Months)

// apparently this doesn't work =(
module ExtensionProperties =
    open ObjectLiterals
    type AddTimeProps with
        member x.Unit2
            with get() = TimeUnit.Months

    let showExtensionPropertyDoesntWork() =
        console.log(ObjectLiterals.parameter)

module EnumedObjectLiteral =
    open System
    type AddTimeProps =
        abstract current : DateTime with get,set
        [<Emit("$0.specialAmount{{=$1}}")>]
        abstract amount : int with get,set
        abstract unit : TimeUnit with get,set
    let parameter = createEmpty<AddTimeProps>
    parameter.current <- DateTime.Now
    parameter.amount <- 30
    parameter.unit <- TimeUnit.Years
    console.log(parameter)

module Pojos =
    // for libraries that require a plain object (like React components)
    [<Pojo>]
    type Person = {name:string;age:int}
    console.log(typeof<Person>)
    type PersonNonPojo = {name2:string;age2:int}
    let person = {name="Mike"; age=35}
    let me = {person with name ="Zaid"}
    let stillMe = {me with age = 20}
    console.log(stillMe)

// author acknowledges this is not idiomatic F#, but it demonstrates some fable features
module DULiteral =
    type Person =
        | Name of string
        | Age of int
    let person = [Name "Mike"; Age 35]
    console.log(person)
    // window["person"] = person;
    defineGlobal "person" person
    let mkPerson (p:Person list) = keyValueList CaseRules.LowerFirst p
    console.log(mkPerson person)

    let p = [Name "Mike"; Age 35; unbox ("someProp", 20); !!("otherProp",40)]
    console.log(mkPerson p)

module LiteralObjectInline =
    let literalObject =
        createObj [
            "prop" ==> "value"
            "anotherProp" ==> 5
        ]
    console.log literalObject
    let literalObject2 =
        createObj [
            "props" ==> "value"
            "anotherProp" ==> 5
            "nested" ==>
                createObj [
                        "nestedProp" ==> "some-value"
                ]
        ]
    console.log(literalObject2)

module ImportsCalls =
    let someJson = "{ \"SpecialProp\": \"hello\" }"
    let t fParse fGetValue =
        let json = someJson

        match fParse json with
        | Some objLiteral ->
            console.log(objLiteral)
            match fGetValue objLiteral "SpecialProp" with
            | Some result -> console.log(sprintf "success:%A" result)
            | None -> console.log "No such property was found"
        | None ->
            console.log("Json parsing did not succeed")
module Imports1 =
    let parseJson : string -> obj option = import "parseJson" "./custom.js"
    let getValue : obj -> string -> obj option = import "getValue" "./custom.js"
    ImportsCalls.t parseJson getValue

module Imports2 =
    [<Import("parseJson", "./custom.js")>]
    let parseJson : string -> obj option = jsNative
    [<Import("getValue", "./custom.js")>]
    let getValue : obj -> string -> obj option = jsNative
    ImportsCalls.t parseJson getValue

// my own experimentation Import modules
module Imports3 =
    [<Import("parseJson", "./custom.js")>]
    let parseJson (json : string) : obj option = jsNative
    [<Import("getValue", "./custom.js")>]
    let getValue (x: obj) (propName:string) : obj option = jsNative
    ImportsCalls.t parseJson getValue

module ImportingAll =
    // [<AbstractClass>]
    type Parse =
        abstract parseJson : string -> obj option
        abstract getValue : obj -> string -> obj option
    let parse = importAll<Parse> "./custom"
    let parse2 : Parse = importAll "./custom"
    [<Import("*", "./custom")>]
    let parse3 : Parse = jsNative
    console.groupCollapsed "ImportingAll"

    let result = parse.parseJson "{ }"
    console.log(result)
    // ugly resulting code
    parse2.parseJson ImportsCalls.someJson
    |> fun x -> console.log(x)
    // pretty resulting code
    console.log(parse2.parseJson ImportsCalls.someJson)

    console.groupEnd()

module ImportSingle =
    let specialValue : string = importDefault "./default.js"
    console.log(specialValue)

module Npms =
    open System
    let leftPad : string -> int -> char -> string = importDefault "left-pad"

    let paddedNumber = leftPad "4" 4 '0'
    console.log(paddedNumber)

    // another overload of the function
    let leftPadWhitespace : string -> int -> string = importDefault "left-pad"
    // this overload pads with spaces
    console.log(leftPadWhitespace "4" 3)

module ErasedDUs =
    open System
    // a function with one parameter which can be a string, int, or dateTime
    // this compiles: but since it doesn't exist, the javascript throws
    // [<Emit("imaginary.func")>]
    // let myFunc (x: U3<string,int,DateTime>): string = jsNative

    let foo() = importAll<unit> "./custom2.js"

    [<Emit("imaginaryfunc($0)")>]
    let imaginaryfunc(x: U3<string,int,DateTime>): string = jsNative // importDefault "./custom2.js"
    let result = imaginaryfunc(!^ "string value")
    console.log(result)
    let result2 = imaginaryfunc(!^ 4)
    console.log(result2)

module FableInteracting =
    [<Emit("Date")>]
    let Date : obj = jsNative
    let instance :obj = createNew Date ()
    console.log(instance)


    let anArray = [| box 4; box "hello"|]
    // apparently fable tries to use new ES2015 TypedArrays for arrays
    let typedArray = [| 4;3;2 |]
    defineGlobal "typedArray" typedArray
    console.log(typedArray)
    let testIsArrayMethod title (f:obj->bool) =
        console.group (sprintf "testIsArrayMethod %s" title)
        console.log(f anArray)
        console.log(f typedArray)
        console.groupEnd()

    [<Emit("Array.isArray")>]
    let isArray : obj -> bool = jsNative
    testIsArrayMethod "isArray" isArray
    // SO says this one is faster and 'best'
    [<Emit("$0.constructor === Array")>]
    let isArray2 (x:'A) : bool = jsNative
    testIsArrayMethod "array2" isArray2

    [<Emit("$0 instanceof Array")>]
    let isArray3 (x:'A) = jsNative
    testIsArrayMethod "isArray3" isArray3

module JsHelpers =
    [<Emit("typeof $0")>]
    let getTypeOf x :string = jsNative



[<Emit("new $0()")>]
let createImported (x:'T) = jsNative


module Jasmine =
    [<Pojo>]
    type Result = {
        pass:bool
        message:string
    }
    [<Pojo>]
    type CustomMatcher<'TActual,'TExpected> = {
        compare:System.Func<'TActual,'TExpected,Result>
    }
    type IJasmine =
        abstract member addMatchers : obj -> unit
    [<Emit("$0 = null;")>]
    let declare (name:string) = jsNative
    [<Emit("beforeEach($0);")>]
    let beforeEach (f:System.Action) = jsNative
    [<Emit("describe($0,$1)")>]
    let describe (x:string) (f: System.Action) :unit = jsNative

    [<Emit("it($0,$1)")>]
    let it (x:string) (f:System.Action) = jsNative
    type IJasmineExpect =
        abstract member toEqual: obj -> unit
        abstract member toBeFalsy: unit -> unit
        abstract member toBeTruthy: unit -> unit
        abstract member toHaveBeenCalledWith: obj -> unit
        abstract member toThrowError: string -> unit

    [<Emit("jasmine")>]
    let jasmine : IJasmine = jsNative

    [<Emit("expect($0)")>]
    let expect o : IJasmineExpect = jsNative
    [<Emit("spyOn($0,$1)")>]
    let spyOn (o:obj) (method:string) : unit = jsNative

    module Sample =
        [<AllowNullLiteralAttribute>]
        type Song =
            abstract member name:string
            // abstract member persistFavoriteStatus:bool with get,set

            // making this a Func enabled passing a reference to the method, for jasmine testing
            abstract member persistFavoriteStatus: System.Func<unit, unit>
        type IJasmineExpect with
            [<Emit("($0).toBePlaying($1)")>]
            member x.toBePlaying (s:Song) = jsNative
        [<Emit("new Song()")>]
        let createSong() : Song = jsNative
        [<AllowNullLiteralAttribute>]
        [<Global>]
        // [<Import("", from="../public/jasmine/src/Player.js")>]
        type Player () =
            member __.play : Song -> unit = jsNative
            member __.pause : unit -> unit = jsNative
            member __.currentlyPlayingSong : Song = jsNative
            member __.isPlaying:bool = jsNative
            member __.resume: unit -> unit = jsNative
            member __.makeFavorite: unit -> unit = jsNative
        // this imports, but the target file doesn't export anything so... no dice
        // importAll "../public/jasmine/src/Player.js" |> ignore

        module SpecHelpers =
            beforeEach(System.Action(fun _ ->
                jasmine.addMatchers (
                    createObj ["toBePlaying", box (System.Func<_>(fun () -> { compare= (System.Func<_,_,_>(fun actual expected ->
                        let player :Player = actual
                        {pass=player.currentlyPlayingSong = expected && player.isPlaying;message=null}
                    ))}))]
                )
            ))
        // appears to have worked, kinda/sorta?
        // could shim it I think? https://webpack.js.org/guides/shimming/
        // importDefault "../public/jasmine/src/Player.js"
        describe "Player" (System.Action(fun _ ->
            let mutable player :Player = null
            let mutable song :Song = null
            beforeEach (System.Action(fun _ ->
                player <- Player()
                song <- createSong ()
            ))
            it "should be able to play a Song" (System.Action(fun () ->
                player.play(song)
                expect(player.currentlyPlayingSong).toEqual(song)
                // custom matcher
                expect(player).toBePlaying(song)
            ))
            describe "when a song has been paused" (System.Action(fun () ->
                beforeEach (System.Action(fun() ->
                    player.play song
                    player.pause()
                    ))

                it "should indicate that the song is currently paused" (System.Action(fun () ->
                    expect(player.isPlaying).toBeFalsy()
                ))
                it "should be possible to resume" (System.Action(fun () ->
                    player.resume()
                    expect(player.isPlaying) .toBeTruthy()
                    expect(player.currentlyPlayingSong).toEqual(song)
                ))
            ))
            it "tells the current song if the user has made it a favorite" (System.Action(fun() ->
                spyOn song "persistFavoriteStatus"
                player.play(song)
                player.makeFavorite()
                // this line fails to pass the delegate, instead it provides a function that calls the delegate
                expect(song.persistFavoriteStatus).toHaveBeenCalledWith(true)
            ))
            describe "resume" (System.Action(fun() ->
                it "should throw an exception if song is already playing" (System.Action(fun() ->
                    player.play(song)
                    expect(System.Action(fun () ->
                            player.resume()
                    )).toThrowError("song is already playing")
                ))
            ))
        ))


open JsHelpers
module JsxHelpers =
    type IReactProps =
        abstract member spread : obj

open JsxHelpers
module PM =
    // Es6 modules automatically enable strict mode
    // useStrict()
    let mapDisplayFromPaymentItemTypeId x =
        match x with
        | "Payment"
        | "EraPayment" -> "Payment"
        | "EraAdjustment" -> "Adjustment"
        | x -> x

    // but in js properties can also be numbers, how to cope?
    [<Erase>]
    type StringOrInt =
        | String of string
        | Number of int
    let isFuncOrNullPropType (props:IReactProps) (propName:StringOrInt) componentName =
        let value:obj =
            box props?(propName)
        let getOrSpread name : obj option =
            // props?name || props.spread && props.spread?name
            if isDefined (props?(name)) then
                Some (box props?(name))
            elif isDefined props.spread then
                Some (box props.spread?(name))
            else
                None
        if isNull value || getTypeOf value = "function" then
            null
        else
            let mutable name = componentName
            // getOrSpread "id"
            // |> Option.iter(fun n -> name <- name + "#" + n)
            match getOrSpread "id" with
            | Some n -> name <- name + "#" + (string n)
            | None -> ()
            name
module ComponentsJsx =
    ()





