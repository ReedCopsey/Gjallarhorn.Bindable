namespace CollectionSample

open Gjallarhorn

type CollectionNav =
    | DisplayRequest of ISignal<Request>
