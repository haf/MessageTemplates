﻿module FsTests.Format

open System
open System.Globalization
open Swensen.Unquote
open Xunit

type Chair() =
    member __.Back with get() = "straight"
    member __.Legs with get() = [|1;2;3;4|]
    override __.ToString() = "a chair"

type Receipt() =
    member __.Sum with get() = 12.345m
    member __.When with get() = System.DateTime(2013, 5, 20, 16, 39, 0)
    override __.ToString() = "a receipt"

// Delegate1 works with tuple arguments. 
type MyDelegate = delegate of (int * int) -> int

[<LangTheory; LangCsFsData>]
let ``a delegate is rendered as a string`` (lang) =
    let myDel = MyDelegate(fun (i1,i2) -> i1+i2)
    let m = render lang "What even would a {del} print?" [box myDel]
    test <@ m = "What even would a \"FsTests.Format+MyDelegate\" print?" @>

[<LangTheory; LangCsFsData>]
let ``a class instance is rendered in simple notation`` (lang) =
    let m = render lang "I sat at {@Chair}" [Chair()]
    test <@ m = "I sat at Chair { Back: \"straight\", Legs: [1, 2, 3, 4] }" @>

[<LangTheory; LangCsFsData>]
let ``a class instance is rendered in simple notation using format provider`` (lang) =
    let m = renderp lang (CultureInfo.GetCultureInfo "fr-FR") "I received {@Receipt}" [Receipt()]
    test <@ m = "I received Receipt { Sum: 12,345, When: 20/05/2013 16:39:00 }" @>

type ChairRecord = { Back:string; Legs: int array }

[<LangTheory; LangCsFsData>]
let ``an F# record object is rendered in simple notation with type`` (lang) =
    let m = render lang "I sat at {@Chair}" [{ Back="straight"; Legs=[|1;2;3;4|] }]
    test <@ m = "I sat at ChairRecord { Back: \"straight\", Legs: [1, 2, 3, 4] }" @>

type ReceiptRecord = { Sum: double; When: System.DateTime }

[<LangTheory; LangCsFsData>]
let ``an F# record object is rendered in simple notation with type using format provider`` (lang) =
    let m = renderp lang (CultureInfo.GetCultureInfo "fr-FR")
                    "I received {@Receipt}" [{ Sum=12.345; When=DateTime(2013, 5, 20, 16, 39, 0) }]
    test <@ m = "I received ReceiptRecord { Sum: 12,345, When: 20/05/2013 16:39:00 }" @>

[<LangTheory; LangCsFsData>]
let ``an object with default destructuring is rendered as a string literal`` (lang) =
    let m = render lang "I sat at {Chair}" [Chair()]
    test <@ m = "I sat at \"a chair\"" @>

[<LangTheory; LangCsFsData>]
let ``an object with stringify destructuring is rendered as a string`` (lang) =
    let m = render lang "I sat at {$Chair}" [Chair()]
    test <@ m = "I sat at \"a chair\"" @>

[<LangTheory; LangCsFsData>]
let ``multiple properties are rendered in order`` (lang) =
    let m = render lang "Just biting {Fruit} number {Count}" [box "Apple"; box 12]
    test <@ m = "Just biting \"Apple\" number 12" @>
    
[<LangTheory; LangCsFsData>]
let ``a template with only positional properties is analyzed and rendered positionally`` (lang) =
    let m = render lang "{1}, {0}" ["world"; "Hello"]
    test <@ m = "\"Hello\", \"world\"" @>

[<LangTheory; LangCsFsData>]
let ``a template with only positional properties uses format provider`` (lang) =
    let m = renderp lang (CultureInfo.GetCultureInfo "fr-FR")
                    "{1}, {0}" [box 12.345; box "Hello"]
    test <@ m = "\"Hello\", 12,345" @>

// Debatable what the behavior should be, here.
[<LangTheory; LangCsFsData>]
let ``a template with names and positionals uses names for all values`` (lang) =
    let m = render lang "{1}, {Place}, {5}" ["world"; "Hello"; "yikes"]
    test <@ m = "\"world\", \"Hello\", \"yikes\"" @>

[<LangTheory; LangCsFsData>]
let ``missing positional parameters render as text like standard formats`` (lang) =
    let m = render lang "{1}, {0}" ["world"]
    test <@ m = "{1}, \"world\"" @>

[<LangTheory; LangCsFsData>]
let ``extra positional parameters are ignored`` (lang) =
    let m = render lang "{1}, {0}" ["world"; "world"; "world"]
    test <@ m = "\"world\", \"world\"" @>
    
[<LangTheory; LangCsFsData>]
let ``multiple properties use format provider`` (lang) =
    let m = renderp lang (CultureInfo.GetCultureInfo "fr-FR")
                    "Income was {Income} at {Date:d}" [box 1234.567; box (DateTime(2013, 5, 20))]
    test <@ m = "Income was 1234,567 at 20/05/2013" @>

[<LangTheory; LangCsFsData>]
let ``format strings are propagated`` (lang) =
    let m = render lang "Welcome, customer {CustomerId:0000}" [12]
    test <@ m = "Welcome, customer 0012" @>

type Cust() =
    let c = Chair()
    member __.Seat = c
    member __.Number = 1234
    override __.ToString() = "1234"

let ``get alignment structure values`` () : obj[] seq = seq {
    let values : obj[] = [| 1234 |]
    yield [| "C#"; values; "cus #{CustomerId,-10}, pleasure to see you";        "cus #1234      , pleasure to see you" |]
    yield [| "C#"; values; "cus #{CustomerId,-10}, pleasure to see you";        "cus #1234      , pleasure to see you" |]
    yield [| "C#"; values; "cus #{CustomerId,-10:000000}, pleasure to see you"; "cus #001234    , pleasure to see you" |]
    yield [| "C#"; values; "cus #{CustomerId,10}, pleasure to see you";         "cus #      1234, pleasure to see you" |]
    yield [| "C#"; values; "cus #{CustomerId,10:000000}, pleasure to see you";  "cus #    001234, pleasure to see you" |]
    yield [| "C#"; values; "cus #{CustomerId,10:0,0}, pleasure to see you";     "cus #     1,234, pleasure to see you" |]
    yield [| "C#"; values; "cus #{CustomerId:0,0}, pleasure to see you";        "cus #1,234, pleasure to see you"      |]
    yield [| "F#"; values; "cus #{CustomerId,-10}, pleasure to see you";        "cus #1234      , pleasure to see you" |]
    yield [| "F#"; values; "cus #{CustomerId,-10:000000}, pleasure to see you"; "cus #001234    , pleasure to see you" |]
    yield [| "F#"; values; "cus #{CustomerId,10}, pleasure to see you";         "cus #      1234, pleasure to see you" |]
    yield [| "F#"; values; "cus #{CustomerId,10:000000}, pleasure to see you";  "cus #    001234, pleasure to see you" |]
    yield [| "F#"; values; "cus #{CustomerId,10:0,0}, pleasure to see you";     "cus #     1,234, pleasure to see you" |]
    yield [| "F#"; values; "cus #{CustomerId:0,0}, pleasure to see you";        "cus #1,234, pleasure to see you"      |]
    
    let values : obj[] = [| Cust() |]
    yield [| "C#"; values; "cus #{$cust:0,0}, pleasure to see you";             "cus #\"1234\", pleasure to see you"    |]
    yield [| "F#"; values; "cus #{$cust:0,0}, pleasure to see you";             "cus #\"1234\", pleasure to see you"    |]
    // formats/alignments don't propagate through to the 'destructured' inside values
    // They only apply to the outer (fully rendered) property text
    yield [| "C#"; values; "cus #{@cust,80:0,0}, pleasure to see you";          "cus #     Cust { Seat: Chair { Back: \"straight\", Legs: [1, 2, 3, 4] }, Number: 1234 }, pleasure to see you"    |]
    yield [| "F#"; values; "cus #{@cust,80:0,0}, pleasure to see you";          "cus #     Cust { Seat: Chair { Back: \"straight\", Legs: [1, 2, 3, 4] }, Number: 1234 }, pleasure to see you"    |]
}

[<LangTheory; MemberData("get alignment structure values")>]
let ``alignment strings are propagated`` (lang:string) (values:obj[]) (template:string) (expected:string) =
    let m = render lang template values
    test <@ m = expected @>

[<LangTheory; LangCsFsData>]
let ``format provider is used`` (lang) =
    let m = renderp lang (CultureInfo.GetCultureInfo "fr-FR")
                   "Please pay {Sum}" [12.345]
    test <@ m = "Please pay 12,345" @>

type Tree = Seq of nums: double list | Leaf of double | Trunk of double * DateTimeOffset * (Tree list)
let Four39PmOn20May2013 = DateTimeOffset(2013, 5, 20, 16, 39, 00, TimeSpan.FromHours 9.5)
type ItemsUnion = ChairItem of c:ChairRecord | ReceiptItem of r:ReceiptRecord

[<LangTheory; LangCsFsData>]
let ``an F# discriminated union object is formatted with provider correctly`` (lang) =
    let provider = CultureInfo.GetCultureInfo "fr-FR"
    let template = "I like {@item1} and {@item2}"
    let values : obj[] = [| ChairItem({ Back="straight"; Legs=[|1;2;3;4|] })
                            ReceiptItem({ Sum=12.345; When=DateTime(2013, 5, 20, 16, 39, 0) }) |]
    let expected = "I like "
                 + "ChairItem { c: ChairRecord { Back: \"straight\", Legs: [1, 2, 3, 4] }, Tag: 0, IsChairItem: True, IsReceiptItem: False }"
                 + " and "
                 + "ReceiptItem { r: ReceiptRecord { Sum: 12,345, When: 20/05/2013 16:39:00 }, Tag: 1, IsChairItem: False, IsReceiptItem: True }"
    Asserts.MtAssert.RenderedAs(lang, template, values, expected, provider)

[<Theory; LangCsFsData>]
let ``Rendered F# DU or Tuple fields are 'null' when depth is 1`` (lang) =
    let provider = (CultureInfo.GetCultureInfo "fr-FR")
    let template = "I like {@item1} and {@item2} and {@item3}"
    let values : obj[] = [| Leaf 12.345
                            Leaf 12.345
                            Trunk (12.345, Four39PmOn20May2013, [Leaf 12.345; Leaf 12.345]) |]
    // all fields should be rendered
    let expected = "I like Leaf { Item: null, Tag: null, IsSeq: null, IsLeaf: null, IsTrunk: null } and "
                 + "Leaf { Item: null, Tag: null, IsSeq: null, IsLeaf: null, IsTrunk: null } and "
                 + "Trunk { Item1: null, Item2: null, Item3: null, Tag: null, IsSeq: null, IsLeaf: null, IsTrunk: null }"
    
    Asserts.MtAssert.RenderedAs(lang, template, values, expected, provider, maxDepth=1)

[<Theory; LangCsFsData>]
let ``Rendered F# DU or Tuple fields on level3 are 'null' when depth is 2`` (lang) =
    let provider = (CultureInfo.GetCultureInfo "fr-FR")
    let template = "I like {@item1} and {@item2} and {@item3} and {@item4}"
    let values : obj[] = [| Leaf 12.345
                            Leaf 12.345
                            Trunk (12.345, Four39PmOn20May2013, [Leaf 12.345; Leaf 12.345])
                            ChairItem { Back="slanted"; Legs=[|1;2;3;4;5|] } |]

    // Render fields deeper than level 2 with 'null' values
    // In this case, only The Trunk.Item3 (Tree list) is after level 2
    let expected = "I like Leaf { Item: 12,345, Tag: 1, IsSeq: False, IsLeaf: True, IsTrunk: False } and "
                 + "Leaf { Item: 12,345, Tag: 1, IsSeq: False, IsLeaf: True, IsTrunk: False } and "
                 + "Trunk { Item1: 12,345, Item2: 20/05/2013 16:39:00 +09:30, Item3: [null, null], Tag: 2, IsSeq: False, IsLeaf: False, IsTrunk: True } and "
                 + "ChairItem { c: ChairRecord { Back: null, Legs: null }, Tag: 0, IsChairItem: True, IsReceiptItem: False }"
    
    Asserts.MtAssert.RenderedAs(lang, template, values, expected, provider, maxDepth=2)

[<LangTheory; LangCsFsData>]
let ``Destructred F# objects captured with a custom destructurer render with format provider`` (lang) =
    let provider = CultureInfo.GetCultureInfo "fr-FR"
    let template = "I like {@item1}
and {@item2}
and {@item3}
and {@item4}"
    let values : obj[] = [| Leaf 12.345
                            Leaf 12.345
                            Trunk (12.345, Four39PmOn20May2013, [Leaf 12.345; Leaf 12.345])
                            Trunk (1.1, Four39PmOn20May2013, [Seq [1.1;2.2;3.3]; Seq [4.4]])
                         |]
    let expected = "I like Leaf { Item: 12,345 }
and Leaf { Item: 12,345 }
and Trunk { Item1: 12,345, Item2: 20/05/2013 16:39:00 +09:30, Item3: [Leaf { Item: 12,345 }, Leaf { Item: 12,345 }] }
and Trunk { Item1: 1,1, Item2: 20/05/2013 16:39:00 +09:30, Item3: [Seq { nums: [1,1, 2,2, 3,3] }, Seq { nums: [4,4] }] }"
    let customFsharpDestr = CsToFs.toFsDestructurer (Destructurama.FSharpTypesDestructuringPolicy())
    let destr = FsMessageTemplates.Capturing.createCustomDestructurer None (Some customFsharpDestr)
    Asserts.MtAssert.RenderedAs(lang, template, values, expected, provider, additionalDestrs=[destr])
