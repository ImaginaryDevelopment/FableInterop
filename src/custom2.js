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
// })(typeof(exports) !== 'undefined' ? inspect('exports',exports) : typeof(global) !== 'undefined' ?inspect('global', global) : window);
})(typeof(exports) !== 'undefined' ? (function (){ console.log('exports!', exports); return exports;}()) : typeof(global) !== 'undefined' ? (function(){ console.log('global!',global); return global}()) : (function(){console.log('window!'); return window; }()));