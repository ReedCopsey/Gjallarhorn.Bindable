namespace CollectionSample

open Gjallarhorn

type CollectionNav =
    | ShowRequestDetails of Request
    | DisplayRequest of ISignal<Request>
    | Login
    | StartProcessing of addNew : bool * processElements : bool
