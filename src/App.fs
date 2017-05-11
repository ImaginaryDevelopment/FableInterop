module FableInterop

open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.Browser

// following https://medium.com/@zaid.naom/f-interop-with-javascript-in-fable-the-complete-guide-ccc5b896a59f

// console.clear here, means any output from previous javascript to load, would be hidden/lost
// console.clear()
console.log("Fable is up and running...")
module Helpers =

    // [<Emit("clearTimeout($0)")>]
    let inline clearTimeout x = Fable.Import.Browser.window.clearTimeout x
    let inline setTimeout x ms = Fable.Import.Browser.window.setTimeout(x,ms)
    [<Emit("throw error($0)")>]
    let jsThrow x : obj -> 'T = jsNative

    [<Emit("undefined")>]
    let undefined : obj = jsNative
    [<Emit("arguments")>]
    let arguments : obj = jsNative
    type Console with
        [<Emit("console.log($1,$2)")>]
        member __.log2 a b = jsNative

    [<Emit("window[$0] = $1")>]
    let defineGlobal (name:string) (x:'A) : unit = jsNative

    [<Emit("$0 === undefined")>]
    // calling this isUndefined1, because I have a different preference for the real isUndefined
    let isUndefined (x: 'a) : bool = jsNative

    [<Emit("$0 != null")>]
    let isDefined (x: 'a) : bool = jsNative


    [<Emit("isNaN($0)")>]
    let isNaN (x:'a):bool = jsNative

    [<Emit("Math.random()")>]
    let getRandom() : float = jsNative

open Helpers

module ParseFloat =
    // other implementations can evaluate $0 twice, if they were complex functions, they would run twice
    // create a function to hold the result of +input and check it for isNaN, return null if so, otherwise the number value
    [<Emit("(x => isNaN(x) ? null : x)(+$0)")>]
    let parseFloat (input : string) : float option = jsNative
    parseFloat("5x")
    |> fun x -> console.log(x)

module JQueryMap =
    type IJQuery =
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

// author acknowledges this is not idiomatic F#, but it demonstrates some fable features

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


module JsHelpers =
    [<Emit("typeof $0")>]
    let getTypeOf x :string = jsNative
    [<Global>]
    module String =
        [<Global>]
        [<Emit("$1.trim()")>]
        let trim (x:string) = x.Trim()


[<Emit("new $0()")>]
let createImported (x:'T) = jsNative




open JsHelpers
module R = Fable.Helpers.React
module JsxHelpers =

    type IReactProps =
        abstract member spread : obj
        // abstract member displayName:string

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
    [<RequireQualifiedAccess>]
    type StringOrInt =
        | String of string
        | Number of int

    // this is a custom PropType checker for react
    let isFuncOrNullPropType (props:IReactProps) (propName:StringOrInt) componentName =
        let value:obj =
            box props?(propName)
        let getOrSpread name : 'T option =
            // props?name || props.spread && props.spread?name
            if isDefined (props?(name)) then
                Some (box props?(name) :?> 'T )
            elif isDefined props.spread then
                Some (box props.spread?(name) :?> 'T)
            else
                None
        if isNull value || getTypeOf value = "function" then
            null
        else
            let mutable name = componentName
            // getOrSpread "id"
            // |> Option.iter(fun n -> name <- name + "#" + n)
            let inline addGetOrSpread (propName:StringOrInt) pre post =
                match getOrSpread propName with
                | Some (n:string) -> name <- name + pre + n + post
                | None -> ()
            // match getOrSpread "id" with
            // | Some n -> name <- name + "#" + (string n)
            // | None -> ()
            addGetOrSpread (StringOrInt.String "id") "#" ""
            addGetOrSpread (StringOrInt.String "data-id") "[data-id=" "]"
            addGetOrSpread (StringOrInt.String "name") "[name=" "]"
            name

module ComponentsJsx =
    // should this be
    // [<Global>]
    type Fable.Import.Browser.Event with
        [<Emit("target.responseText")>]
        member x.targetResponseText :string = jsNative
    type AjaxEvent = Fable.Import.Browser.Event
    type XMLHttpRequest with
        [<Emit("$0.send()")>]
        member x.send() = jsNative
    let mutable IsAjaxWrapperDebug = false
    let debugAjaxWrapper ([<Erase>]any:obj) =
        if IsAjaxWrapperDebug then
            console.log(arguments)
    type JsonString = string

    module AjaxAssistants =

        [<Pojo>]
        type AjaxWrapperState = {data:JsonString; searchError:string; loading:bool}
        [<Pojo>]
        type AjaxWrapperProps<'T> = {getUrl:string;render:System.Func<'T,React.ReactElement>}
        type AjaxWrapper(props) =
            inherit React.Component<AjaxWrapperProps<AjaxWrapperState>,AjaxWrapperState>(props)
            do base.setInitState({data=null; searchError=null;loading=true})

            member x.componentWillMount() =
                debugAjaxWrapper("AjaxWrapper: componentWillMount")
            member x.onSearchFailed searchText =
                debugAjaxWrapper("AjaxWrapper:onSearchFailed")
                console.warn("ajax failed")
                x.setState({x.state with data=null; searchError= "failed to search for " + (string searchText)})
            member x.onSearchResults (evt: AjaxEvent) =
                // does this indicate we need another generic param on this type? perhaps on the props type so the renderer accepts it
                let model : obj = Fable.Core.JsInterop.ofJson evt.targetResponseText
                debugAjaxWrapper("AjaxWrapper: onSearchResults", model, evt)
            member x.sendSearch() =
                debugAjaxWrapper("AjaxWrapper: about to fetch")
                let oReq : XMLHttpRequest = createImported XMLHttpRequest
                oReq.addEventListener_load (System.Func<_,_>(fun evt -> x.onSearchResults evt;undefined))
                oReq.addEventListener_error (System.Func<_,_>(fun eEvt -> x.onSearchFailed(box eEvt); undefined))
                oReq.``open``("GET", x.props.getUrl)
                oReq.send()
                oReq

            member x.Render() : React.ReactElement =
                debugAjaxWrapper("AjaxWrapper: rendering", x.state)
                let rendering = props.render.Invoke(x.state)
                if isDefined rendering then
                    rendering
                else
                    R.div list.Empty [R.str "ajax wrapper failed to render"]

    open AjaxAssistants
    let ajaxWrapperTest = AjaxAssistants.AjaxWrapper({getUrl=null;render=null})
    // we have F# type system, do we need propTypes anymore?
    // AjaxWrapper?propTypes <- createObj ["render", ]

    // expose it out as a global type, so we can inspect it/test it

    [<Emit("AjaxAssistants.AjaxWrapper")>]
    let ajaxwrapperTypeName:obj = jsNative

    window?AjaxWrapper <- ajaxwrapperTypeName

    [<Pojo>]
    type AjaxRenderWrapperProps<'T> = {title:string; loading:bool; data:'T; renderData:System.Func<'T,React.ReactElement>}
// why does this use a type with props.renderData and not props.render like the AjaxWrapper does?
    let AjaxRenderWrapper = System.Func<AjaxRenderWrapperProps<'T>,_>(fun (props:AjaxRenderWrapperProps<'T>) ->
        if isDefined props?searchError || (not props.loading && isUndefined props.data) then
            R.div [R.Props.ClassName "text-danger"] [props.title |> R.str]
        elif props.loading then
            R.div [R.Props.ClassName "text-warning"] ["Loading" + props.title + "..." |> R.str]
        else
            props.renderData.Invoke(props.data)
    )

    [<Pojo>]
    type AjaxComponentProps<'T> = {title:string;loading:bool;renderData:System.Func<'T,React.ReactElement>; getUrl:string}
    // translated from Ajax component
    let ajaxComponent props =
        let renderGiftWrapping = (System.Func<_,_>(fun (state:AjaxWrapperState) ->
            let p = {title=props.title; loading=state.loading; data=state.data;renderData = props.renderData}
            AjaxRenderWrapper.Invoke(p)))
        // type AjaxWrapperProps = {getUrl:string;renderer:System.Func<obj,React.ReactElement>}
        let p :AjaxWrapperProps<_> = {getUrl=props.getUrl; render=renderGiftWrapping}
        AjaxWrapper(p)

    ()

module AllHelpers =
    [<Erase>]
    [<RequireQualifiedAccess>]
    type JsNumber =
        | Int of int
        | Float of float
    [<Erase>]
    [<RequireQualifiedAccess>]
    type NumberOrNaN =
        |Number of JsNumber
        |NaN
    [<Erase>]
    [<RequireQualifiedAccess>]
    type NumberOrString =
        | Number of JsNumber
        | String of string
    // type System.Object with
    //     [<Emit("Object.keys($0)")>]
    //     static member keys (x:obj) : string[] = jsNative
    [<Global>]
    module Number =
        let isNaN : obj -> bool = jsNative

    [<Emit("Object.keys($0)")>]
    [<Global>]
    let keys x: string[] = jsNative

    [<Emit("$0 in $1")>]
    let isIn (x:obj) (prop:obj) :bool = jsNative

    [<Emit("+$0")>]
    let convertToNumber (x:obj) : NumberOrNaN = jsNative
    // [<Emit("Object")>]
    // module Object =

    //     [<Emit("Object.keys($0)")>]
    //     let keys (x:obj) : string array = jsNative

    let mutable loggedStorageFailure = false

    // behavior is undefined for arrays.. what should it do?
    let getNumberOrDefault (x:obj) (defaultValue:int) =
        // if isNaN(convertToNumber x) || isUndefined x || isNull x then
        if Number.isNaN (convertToNumber x) || isUndefined x || isNull x then
            box defaultValue
        else box <| convertToNumber x
    // [<Emit("Object.keys($0)")>]
    // let sourceKeys src : obj[] = jsNative
    let copyObject (source:obj) toMerge defaultValue =
        if isUndefined source || isNull source then
            defaultValue
        else
            let target = if isDefined toMerge then toMerge else createEmpty
            // System.Object.keys source
            // keys source
            Seq.filter(fun propName -> not (isIn propName target)) (Fable.Import.JS.Object.keys source)
            |> Seq.iter(fun propName -> target?(propName) <- source?(propName))
            target
    let debounce =
        let mutable timer = 0.
        System.Action<obj,float>(fun callback ms ->
            if getTypeOf callback <> "function" then
                Helpers.jsThrow callback |> ignore
            clearTimeout timer
            // clearTimeout timer
            timer <- setTimeout callback ms
        )
    ()

[<Emit("AllHelpers")>]
let allHelpers : obj = jsNative
defineGlobal "AllHelpers" allHelpers
module Loot = ()
module Adapters = ()
module Data = ()
module FormationCalc = ()
[<CompiledName("JsxHelpers")>]
module CJsxHelpers = ()
module TalentCalc = ()
module EpTracker = ()

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
        abstract member toBe: obj -> unit
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
    module AllHelpersTests =
        open AllHelpers
        let helloWorldP = ["Hello",box "World"]
        let helloWorldObj = createObj helloWorldP
        let copy src m def = AllHelpers.copyObject src m def
        describe "AllHelpers" (System.Action(fun () ->
            describe "getNumberOrDefault" (System.Action (fun () ->
                // console.group "getNumberOrDefault"
                it "Should be able to get a number from a string" (System.Action(fun () ->
                    let expected = 1
                    let actual = getNumberOrDefault "1" 5
                    expect(expected).toEqual(actual)
                ))
                it "Should return the defaultValue when the target is undefined" (System.Action(fun () ->
                    let expected = 1
                    let actual = getNumberOrDefault undefined expected
                    expect(expected).toEqual(actual)
                ))
                it "Should return the defaultValue when the target is null" (System.Action(fun () ->
                    let expected = 1
                    let actual = getNumberOrDefault null expected
                    expect(expected).toEqual(actual)
                ))
                it "Should return the defaultValue when the target is interesting value \"5e\"" (System.Action(fun () ->
                    let expected = 1
                    let actual = getNumberOrDefault "5e" expected
                    expect(expected).toEqual(actual)
                ))
                // console.groupEnd()
            ))
            describe "copyObject" (System.Action(fun () ->
                it "Should copy an object" (System.Action(fun () ->
                    let expected = helloWorldObj
                    let actual : obj = copy expected undefined undefined
                    expect(expected).toEqual(actual)

                ))
                it "Should return undefined when source is undefined" (System.Action(fun () ->
                    let expected = undefined
                    let actual = copy expected undefined undefined
                    expect(expected).toEqual(actual)
                ))
                it "Should return defaultValue when source is null" (System.Action(fun () ->
                    let expected = helloWorldObj
                    let actual = copy null undefined expected
                    expect(expected).toEqual(actual)
                ))
                it "Should be able to merge two objects, and not use the defaultValue" (System.Action(fun () ->
                    let src = helloWorldObj
                    let toMergeP = ["World",box "Goodbye"]
                    let expected = createObj (toMergeP@helloWorldP)
                    let actual = copy src (createObj toMergeP) (box 1)
                    expect(expected).toEqual(actual)
                ))
                it "Should not return the same object" (System.Action(fun () ->
                    let src = createEmpty
                    let actual = copy src null null
                    actual?("Hello") <- "World"
                    expect(src?("Hello")).toBe(undefined)
                    ()
                ))
            ))
        ))
