(function(app){
    var imaginaryfunc = app.imaginaryfunc = function(multiType) {
            if(typeof(multiType) == "number"){
                console.log("number!");
            }
            else if(typeof(multiType) == "string"){
                console.log("string!");
            }
            else if(typeof(multiType) == "object" && (multiType instanceof Date)){
                console.log("Date!");
            }
            else{
                console.log("unmatched");
            }
            return {imaginaryFuncRan:true};
    }
})(typeof(exports) !== 'undefined' ? exports : typeof(global) !== 'undefined' ? global : window);