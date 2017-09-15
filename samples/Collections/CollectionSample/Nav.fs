namespace CollectionSample

open Gjallarhorn

type CollectionNav =
    | DisplayRequest of ISignal<Request>
    | Login
    | StartProcessing of addNew : bool * processElements : bool
