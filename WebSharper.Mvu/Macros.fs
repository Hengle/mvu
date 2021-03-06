// $begin{copyright}
//
// This file is part of WebSharper
//
// Copyright (c) 2008-2018 IntelliFactory
//
// Licensed under the Apache License, Version 2.0 (the "License"); you
// may not use this file except in compliance with the License.  You may
// obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
// implied.  See the License for the specific language governing
// permissions and limitations under the License.
//
// $end{copyright}
module private WebSharper.Mvu.Macros

#nowarn "25" // incomplete match on `let [x; y] = ...`

open WebSharper
open WebSharper.Core
open WebSharper.Core.AST
module I = WebSharper.Core.AST.IgnoreSourcePos

[<AutoOpen>]
module private Impl =
    let meth' name param ret gen = Hashed ({ MethodName = name; Parameters = param; ReturnType = ret; Generics = gen } : MethodInfo)
    let meth g name param ret =
        match List.length g with
        | 0 -> NonGeneric (meth' name param ret 0)
        | n -> Generic (meth' name param ret n) g
    let TP = TypeParameter
    let T0 = TP 0
    let T1 = TP 1
    let T2 = TP 2
    let T = ConcreteType
    let (^->) x y = FSharpFuncType(x, y)

    let wsui n = Hashed ({ Assembly = "WebSharper.UI"; FullName = "WebSharper.UI." + n } : TypeDefinitionInfo)
    let mVar = NonGeneric (wsui "Var")
    let tVar t = GenericType (wsui "Var`1") [t]
    let tMvu n = Hashed ({ Assembly = "WebSharper.Mvu"; FullName = "WebSharper.Mvu." + n } : TypeDefinitionInfo)
    let tApp ts = Generic (tMvu "App`3") ts
    let pAppVar   = meth []     "get_Var" []                                     (tVar T1)
    let fLens t u = meth [t; u] "Lens"    [tVar T0; T0 ^-> T1; T0 ^-> T1 ^-> T0] (tVar T1)

type WithRouting() =
    inherit Macro()

    let tryGetId (call: MacroCall) e =
        match e with
        | I.Var i ->
            match call.BoundVars.TryGetValue(i) with
            | true, x -> x, fun r -> MacroUsedBoundVar(i, r)
            | false, _ -> e, id
        | _ -> e, id

    override this.TranslateCall(call) =
        let [router; getter; app] = call.Arguments
        let getter, wrap = tryGetId call getter
        match WebSharper.UI.Macros.Lens.MakeSetter call.Compilation getter with
        | MacroOk setter ->
            let tRoute :: ([_; tModel; _] as appTArgs) = call.Method.Generics
            let var = Call (Some app, tApp appTArgs, pAppVar, [])
            let lensedVar = Call (None, mVar, fLens tModel tRoute, [var; getter; setter])
            let f =
                { call.Method with
                    Entity = Hashed {
                        call.Method.Entity.Value with
                            MethodName = "withRouting"
                            Parameters = tVar T0 :: call.Method.Entity.Value.Parameters } }
            Call (None, call.DefiningType, f, lensedVar :: call.Arguments)
            |> MacroOk
        | x -> x
