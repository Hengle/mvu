namespace WebSharper.Mvu

open WebSharper
open WebSharper.Sitelets
open WebSharper.UI

/// A function that dispatches a message to the update function.
type Dispatch<'Message> = 'Message -> unit

/// An MVU application.
[<Sealed>]
type App<'Message, 'Model, 'Rendered>

/// An action to take as a result of the Update function.
type Action<'Message, 'Model> =
    /// Don't take any action.
    | DoNothing
    /// Set the model to the given value.
    | SetModel of 'Model
    /// Update the model based on the given function.
    /// Useful if several combined actions need to update the state.
    | UpdateModel of ('Model -> 'Model)
    /// Run the given command synchronously. The command can dispatch subsequent actions.
    | Command of (Dispatch<'Message> -> unit)
    /// Run the given command asynchronously. The command can dispatch subsequent actions.
    | CommandAsync of (Dispatch<'Message> -> Async<unit>)
    /// Run several actions in sequence.
    | CombinedAction of list<Action<'Message, 'Model>>

    /// Run several actions in sequence.
    static member (+)
        : Action<'Message, 'Model>
        * Action<'Message, 'Model>
       -> Action<'Message, 'Model>

[<AutoOpen>]
module Action =

    /// Run the given asynchronous job then dispatch a message based on its result.
    val DispatchAsync<'T, 'Message, 'Model>
        : toMessage: ('T -> 'Message)
       -> action: Async<'T>
       -> Action<'Message, 'Model>

[<Sealed>]
type Page<'Message, 'Model> =

    /// <summary>
    /// Create a reactive page.
    /// </summary>
    /// <param name="key">Get the identifier of the current endpoint. A new instance of the page is only created for different values of the key.</param>
    /// <param name="render">Render the page itself.</param>
    /// <param name="attrs">Attributes to add to the wrapping div.</param>
    /// <param name="keepInDom">If true, don't remove the page from the DOM when hidden.</param>
    /// <param name="usesTransition">Pass true if this page uses CSS transitions to appear and disappear.</param>
    static member Reactive<'EndPointArgs, 'Key>
        : key: ('EndPointArgs -> 'Key)
        * render: ('Key -> Dispatch<'Message> -> View<'Model> -> Doc)
        * ?attrs: seq<Attr>
        * ?keepInDom: bool
        * ?usesTransition: bool
       -> ('EndPointArgs -> Page<'Message, 'Model>)
        when 'Key : equality

    /// <summary>
    /// Create a reactive page. A new instance of the page is created for different values of the endpoint args.
    /// </summary>
    /// <param name="render">Render the page itself.</param>
    /// <param name="attrs">Attributes to add to the wrapping div.</param>
    /// <param name="keepInDom">If true, don't remove the page from the DOM when hidden.</param>
    /// <param name="usesTransition">Pass true if this page uses CSS transitions to appear and disappear.</param>
    static member Create<'EndPointArgs>
        : render: ('EndPointArgs -> Dispatch<'Message> -> View<'Model> -> Doc)
        * ?attrs: seq<Attr>
        * ?keepInDom: bool
        * ?usesTransition: bool
       -> ('EndPointArgs -> Page<'Message, 'Model>)
        when 'EndPointArgs : equality

    /// <summary>
    /// Create a reactive page. A single instance of the page is (lazily) created.
    /// </summary>
    /// <param name="render">Render the page itself.</param>
    /// <param name="attrs">Attributes to add to the wrapping div.</param>
    /// <param name="keepInDom">If true, don't remove the page from the DOM when hidden.</param>
    /// <param name="usesTransition">Pass true if this page uses CSS transitions to appear and disappear.</param>
    static member Single
        : render: (Dispatch<'Message> -> View<'Model> -> Doc)
        * ?attrs: seq<Attr>
        * ?keepInDom: bool
        * ?usesTransition: bool
       -> (unit -> Page<'Message, 'Model>)

/// Bring together the Model-View-Update system and augment it with extra capabilities.
module App =

    /// <summary>
    /// Create an MVU application.
    /// </summary>
    /// <param name="initModel">The initial value of the model.</param>
    /// <param name="update">Computes the new model on every message.</param>
    /// <param name="render">Renders the application based on a reactive view of the model.</param>
    val CreateSimple<'Message, 'Model, 'Rendered>
        : initModel: 'Model
       -> update: ('Message -> 'Model -> 'Model)
       -> render: (Dispatch<'Message> -> View<'Model> -> 'Rendered)
       -> App<'Message, 'Model, 'Rendered>

    /// <summary>
    /// Create an MVU application.
    /// </summary>
    /// <param name="initModel">The initial value of the model.</param>
    /// <param name="update">Computes the new model and/or dispatches commands on every message.</param>
    /// <param name="render">Renders the application based on a reactive view of the model.</param>
    val Create<'Message, 'Model, 'Rendered>
        : initModel: 'Model
       -> update: ('Message -> 'Model -> Action<'Message, 'Model>)
       -> render: (Dispatch<'Message> -> View<'Model> -> 'Rendered)
       -> App<'Message, 'Model, 'Rendered>

    /// <summary>
    /// Create an MVU application using paging.
    /// </summary>
    /// <param name="initModel">The initial value of the model.</param>
    /// <param name="update">Computes the new model on every message.</param>
    /// <param name="render">Renders the application based on a reactive view of the model.</param>
    val CreateSimplePaged<'Message, 'Model>
        : initModel: 'Model
       -> update: ('Message -> 'Model -> 'Model)
       -> render: ('Model -> Page<'Message, 'Model>)
       -> App<'Message, 'Model, Doc>

    /// <summary>
    /// Create an MVU application using paging.
    /// </summary>
    /// <param name="initModel">The initial value of the model.</param>
    /// <param name="update">Computes the new model and/or dispatches commands on every message.</param>
    /// <param name="render">Renders the application based on a reactive view of the model.</param>
    val CreatePaged<'Message, 'Model>
        : initModel: 'Model
       -> update: ('Message -> 'Model -> Action<'Message, 'Model>)
       -> render: ('Model -> Page<'Message, 'Model>)
       -> App<'Message, 'Model, Doc>

    /// <summary>
    /// Add URL hash routing to an application's model.
    /// </summary>
    /// <param name="router">The URL router.</param>
    /// <param name="getRoute">Where the current endpoint is stored in the model. Must be a record field access.</param>
    /// <param name="app">The application.</param>
    val WithRouting<'Route, 'Message, 'Model, 'Rendered>
        : router: Router<'Route>
       -> getRoute: ('Model -> 'Route)
       -> app: App<'Message, 'Model, 'Rendered>
       -> App<'Message, 'Model, 'Rendered>
        when 'Route : equality

    /// <summary>
    /// Add URL hash routing to an application's model.
    /// </summary>
    /// <param name="router">The URL router.</param>
    /// <param name="getRoute">How to get the current endpoint from the model.</param>
    /// <param name="setRoute">How to set the current endpoint in the model.</param>
    /// <param name="app">The application.</param>
    val WithCustomRouting<'Route, 'Message, 'Model, 'Rendered>
        : router: Router<'Route>
       -> getRoute: ('Model -> 'Route)
       -> setRoute: ('Route -> 'Model -> 'Model)
       -> app: App<'Message, 'Model, 'Rendered>
       -> App<'Message, 'Model, 'Rendered>
        when 'Route : equality

    /// <summary>
    /// Add Local Storage capability to the application.
    /// On startup, load the model from local storage at the given key,
    /// or keep the initial model if there is nothing stored yet.
    /// On every update, store the model in local storage.
    /// </summary>
    /// <param name="key">The local storage key</param>
    /// <param name="app">The application</param>
    val WithLocalStorage<'Message, 'Model, 'Rendered>
        : key: string
       -> app: App<'Message, 'Model, 'Rendered>
       -> App<'Message, 'Model, 'Rendered>

    /// Run the given action on startup.
    val WithInitAction<'Message, 'Model, 'Rendered>
        : action: Action<'Message, 'Model>
       -> app: App<'Message, 'Model, 'Rendered>
       -> App<'Message, 'Model, 'Rendered>

    /// Dispatch the given message on startup.
    val WithInitMessage<'Message, 'Model, 'Rendered>
        : message: 'Message
       -> app: App<'Message, 'Model, 'Rendered>
       -> App<'Message, 'Model, 'Rendered>

    /// Run the application.
    val Run<'Message, 'Model, 'Rendered>
        : app: App<'Message, 'Model, 'Rendered>
       -> 'Rendered

    /// <summary>
    /// Add RemoteDev capability to the application.
    /// Allows inspecting the model's history and time-travel debugging.
    /// </summary>
    /// <param name="options">The RemoteDev options</param>
    /// <param name="app">The application</param>
    val WithRemoteDev
        : options: RemoteDev.Options
       -> app: App<'Message, 'Model, 'Rendered>
       -> App<'Message, 'Model, 'Rendered>
