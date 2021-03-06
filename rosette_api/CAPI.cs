﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Text.RegularExpressions;

namespace rosette_api {
    /// <summary>
    /// Enum to provide feature options for Morphology
    /// </summary>
    public enum MorphologyFeature { 
        /// <summary>provide complete morphology</summary>
        complete, 
        /// <summary>provide lemmas</summary>
        lemmas, 
        /// <summary>provide parts of speech</summary>
        partsOfSpeech, 
        /// <summary>provide compound components</summary>
        compoundComponents, 
        /// <summary>provide han readings</summary>
        hanReadings 
    };

    /// <summary>C# Rosette API.
    /// <para>
    /// Primary class for interfacing with the Rosette API
    /// @copyright 2014-2015 Basis Technology Corporation.
    /// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance
    /// with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
    /// Unless required by applicable law or agreed to in writing, software distributed under the License is
    /// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    /// See the License for the specific language governing permissions and limitations under the License.
    /// </para>
    /// </summary>
    public class CAPI {
        private const string CONCURRENCY_HEADER = "X-RosetteAPI-Concurrency";

        /// <summary>
        /// Internal string to hold the uri ending for each endpoint. 
        /// Set when an endpoint is called.
        /// </summary>
        private string _uri = null;

        /// <summary>
        /// Internal container for options
        /// </summary>
        private Dictionary<string, object> _options;

        /// <summary>
        /// Internal container for custom headers
        /// </summary>
        private Dictionary<string, string> _customHeaders;

        /// <summary>C# API class
        /// <para>Rosette Python Client Binding API; representation of a Rosette server.
        /// Instance methods of the C# API provide communication with specific Rosette server endpoints.
        /// Requires user_key to start and has 3 additional parameters to be specified. 
        /// Will run a Version Check against the Rosette Server. If the version check fails, a 
        /// RosetteException will be thrown. 
        /// </para>
        /// </summary>
        /// <param name="user_key">string: API key required by the Rosette server to allow access to endpoints</param>
        /// <param name="uristring">(string, optional): Base URL for the HttpClient requests. If none is given, will use the default API URI</param>
        /// <param name="maxRetry">(int, optional): Maximum number of times to retry a request on HttpResponse error. Default is 3 times.</param> 
        /// <param name="client">(HttpClient, optional): Forces the API to use a custom HttpClient.</param>
        public CAPI(string user_key, string uristring = "https://api.rosette.com/rest/v1/", int maxRetry = 5, HttpClient client = null) {
            UserKey = user_key;
            URIstring = (uristring == null) ? "https://api.rosette.com/rest/v1/" : uristring;
            MaxRetry = (maxRetry == 0) ? 1 : maxRetry;
            MillisecondsBetweenRetries = 5000;
            Debug = false;
            Timeout = 0;
            Client = client;
            Concurrency = System.Net.ServicePointManager.DefaultConnectionLimit;
            _options = new Dictionary<string, object>();
            _customHeaders = new Dictionary<string, string>();
        }

        /// <summary>UserKey
        /// <para>
        /// Getter, Setter for the UserKey
        /// UserKey: API key required by the Rosette Server
        /// Allows users to change their UserKey later (e.g. if instantiated class incorrectly)
        /// </para>
        /// </summary>
        public string UserKey { get; set; }

        /// <summary>URIstring
        /// <para>
        /// Getter, Setter for the URIstring
        /// URIstring: Base URI for the HttpClient.
        /// Allows users to change their URIstring later (e.g. if instantiated class incorrectly)
        /// </para>
        /// </summary>
        public string URIstring { get; set; }

        /// <summary>Version
        /// <para>
        /// Getter, Setter for the Version
        /// Version: Internal Server Version number.
        /// </para>
        /// </summary>
        public static string Version {
            get { return typeof(CAPI).Assembly.GetName().Version.ToString(); }
        }
        /// <summary>MaxRetry
        /// <para>
        /// Getter, Setter for the MaxRetry
        /// MaxRetry: Maximum number of times to retry a request on HTTPResponse error.
        /// Allows users to change their MaxRetry later (e.g. if instantiated class incorrectly)
        /// </para>
        /// </summary>
        public int MaxRetry { get; set; }

        /// <summary>MillisecondsBetweenRetries
        /// <para>
        /// Getter, Setter for the MillisecondsBetweenRetries
        /// MillisecondsBetweenRetries: milliseconds between retry attempts
        /// </para>
        /// </summary>
        public int MillisecondsBetweenRetries { get; set; }

        /// <summary>Client
        /// <para>
        /// Getter, Setter for the Client
        /// Client: Forces the API to use a custom HttpClient.
        /// </para>
        /// </summary>
        public HttpClient Client { get; set; }

        /// <summary>Concurrency
        /// Gets and sets the maximum number of concurrent calls for the plan associated with your API key
        /// </summary>
        public int Concurrency { get; set; }

        /// <summary>Debug
        /// <para>
        /// Getter, Setter for the Debug
        /// Debug: Sets the debug mode parameter for the Rosette server.
        /// </para>
        /// </summary>
        public bool Debug { get; set; }

        /// <summary>Timeout
        /// <para>
        /// Getter, Setter for the Timeout
        /// Timeout: Sets the Timeout for the HttpClient.
        /// </para>
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// Sets the named option to the provided value
        /// </summary>
        /// <param name="name">string option name</param>
        /// <param name="value">object value</param>
        public void SetOption(string name, object value) {
            if (_options.ContainsKey(name) && value == null) {
                _options.Remove(name);
                return;
            }
            _options[name] = value;

        }

        /// <summary>
        /// Gets the requested option
        /// </summary>
        /// <param name="name">string option name</param>
        /// <returns>object value if exists</returns>
        public object GetOption(string name) {
            if (_options.ContainsKey(name)) {
                return _options[name];
            }
            else {
                return null;
            }
        }

        /// <summary>
        /// Clears all of the options
        /// </summary>
        public void ClearOptions() {
            _options.Clear();
        }

        /// <summary>
        /// Sets a custom header to the named value
        /// </summary>
        /// <param name="key">string custom header key</param>
        /// <param name="value">custom header value</param>
        public void SetCustomHeaders(string key, string value) {
            if (_customHeaders.ContainsKey(key) && value == null) {
                _customHeaders.Remove(key);
                return;
            }
            _customHeaders[key] = value;
        }

        /// <summary>
        /// Gets the custom headers
        /// </summary>
        /// <returns>dictionary of custom headers</returns>
        public Dictionary<string, string> GetCustomHeaders() {
            return _customHeaders;
        }

        /// <summary>
        /// Clears all of the custom headers
        /// </summary>
        public void ClearCustomHeaders() {
            _customHeaders.Clear();
        }

        private Dictionary<object, object> appendOptions(Dictionary<object, object> dict) {
            if (_options.Count > 0) {
                dict["options"] = _options;
            }
            return dict;
        }

        /// <summary>Categories
        /// <para>
        /// (POST)Categories Endpoint: Returns an ordered list of categories identified in the input. The categories are Tier 1 contextual categories defined in the QAG Taxonomy.
        /// </para>
        /// </summary>
        /// <param name="content">(string, optional): Input to process (JSON string or base64 encoding of non-JSON string)</param>
        /// <param name="language">(string, optional): Language: ISO 639-3 code (ignored for the /language endpoint)</param>
        /// <param name="contentType">(string, optional): not used at this time</param>
        /// <param name="contentUri">(string, optional): URI to accessible content (content and contentUri are mutually exclusive)</param>
        /// <param name="genre">(string, optional): genre to categorize the input data</param>
        /// <returns>CategoriesResponse containing the results of the request.
        /// The response is the contextual categories identified in the input.
        /// </returns>
        public CategoriesResponse Categories(string content = null, string language = null, string contentType = null, string contentUri = null, string genre = null)
        {
            _uri = "categories";
            return Process<CategoriesResponse>(content, language, contentType, contentUri, genre);
        }

        /// <summary>Categories
        /// <para>
        /// (POST)Categories Endpoint: Returns an ordered list of categories identified in the input. The categories are Tier 1 contextual categories defined in the QAG Taxonomy.
        /// </para>
        /// </summary>
        /// <param name="dict">Dictionary&lt;object, object&gt;: Dictionary containing parameters as (key,value) pairs</param>
        /// <returns>CategoriesResponse containing the results of the request.
        /// The response is the contextual categories identified in the input.
        /// </returns>
        public CategoriesResponse Categories(Dictionary<object, object> dict)
        {
            _uri = "categories";
            return getResponse<CategoriesResponse>(SetupClient(), new JavaScriptSerializer().Serialize(appendOptions(dict)));
        }

        /// <summary>Categories
        /// <para>
        /// (POST)Categories Endpoint: Returns an ordered list of categories identified in the input. The categories are Tier 1 contextual categories defined in the QAG Taxonomy.
        /// </para>
        /// </summary>
        /// <param name="file">RosetteFile: RosetteFile Object containing the file (and possibly options) to upload</param>
        /// <returns>RosetteResponse containing the results of the request.
        /// The response is the contextual categories identified in the input.
        /// </returns>
        public CategoriesResponse Categories(RosetteFile file) {
            _uri = "categories";
            return Process<CategoriesResponse>(file);
        }

        /// <summary>Entity
        /// <para>
        /// (POST)Entity Endpoint: Returns each entity extracted from the input.
        /// </para>
        /// </summary>
        /// <param name="content">(string, optional): Input to process (JSON string or base64 encoding of non-JSON string)</param>
        /// <param name="language">(string, optional): Language: ISO 639-3 code (ignored for the /language endpoint)</param>
        /// <param name="contentType">(string, optional): not used at this time</param>
        /// <param name="contentUri">(string, optional): URI to accessible content (content and contentUri are mutually exclusive)</param>
        /// <param name="genre">(string, optional): genre to categorize the input data</param>
        /// <returns>EntitiesResponse containing the results of the request. 
        /// </returns>
        public EntitiesResponse Entity(string content = null, string language = null, string contentType = null, string contentUri = null, string genre = null)
        {
            _uri = "entities";
            return Process<EntitiesResponse>(content, language, contentType, contentUri, genre);
        }

        /// <summary>Entity
        /// <para>
        /// (POST)Entity Endpoint: Returns each entity extracted from the input.
        /// </para>
        /// </summary>
        /// <param name="dict">Dictionary&lt;object, object&gt;: Dictionary containing parameters as (key,value) pairs</param>
        /// <returns>EntitiesResponse containing the results of the request. 
        /// </returns>
        public EntitiesResponse Entity(Dictionary<object, object> dict)
        {
            _uri = "entities";
            return getResponse<EntitiesResponse>(SetupClient(), new JavaScriptSerializer().Serialize(appendOptions(dict)));
        }

        /// <summary>Entity
        /// <para>
        /// (POST)Entity Endpoint: Returns each entity extracted from the input.
        /// </para>
        /// </summary>
        /// <param name="file">RosetteFile: RosetteFile Object containing the file (and possibly options) to upload</param>
        /// <returns>EntitiesResponse containing the results of the request. 
        /// </returns>
        public EntitiesResponse Entity(RosetteFile file) {
            _uri = "entities";
            return Process<EntitiesResponse>(file);
        }

        /// <summary>Info
        /// <para>
        /// (GET)Info Endpoint: Response is a JSON string with Rosette API information including buildNumber, name, version, and buildTime.
        /// </para>
        /// </summary>
        /// <returns>InfoResponse containing the results of the info GET.</returns>
        public InfoResponse Info()
        {
            _uri = "info";
            return getResponse<InfoResponse>(SetupClient());
        }

        /// <summary>Language
        /// <para>
        /// (POST)Language Endpoint: Returns list of candidate languages.
        /// </para>
        /// </summary>
        /// <param name="content">(string, optional): Input to process (JSON string or base64 encoding of non-JSON string)</param>
        /// <param name="language">(string, optional): Language: ISO 639-3 code (ignored for the /language endpoint)</param>
        /// <param name="contentType">(string, optional): MIME type of the input (required for base64 content; if content type is unknown, set to "application/octet-stream")</param>
        /// <param name="contentUri">(string, optional): URI to accessible content (content and contentUri are mutually exclusive)</param>
        /// <param name="genre">(string, optional): genre to categorize the input data</param>
        /// <returns>LanguageIdentificationResponse containing the results of the request. 
        /// The response is an ordered list of detected languages.
        /// </returns>
        public LanguageIdentificationResponse Language(string content = null, string language = null, string contentType = null, string contentUri = null, string genre = null)
        {
            _uri = "language";
            return Process<LanguageIdentificationResponse>(content, language, contentType, contentUri, genre);
        }

        /// <summary>Language
        /// <para>
        /// (POST)Language Endpoint: Returns list of candidate languages.
        /// </para>
        /// </summary>
        /// <param name="dict">Dictionary&lt;object, object&gt;: Dictionary containing parameters as (key,value) pairs</param>
        /// <returns>LanguageIdentificationResponse containing the results of the request. 
        /// The response is an ordered list of detected languages.
        /// </returns>
        public LanguageIdentificationResponse Language(Dictionary<object, object> dict)
        {
            _uri = "language";
            return getResponse<LanguageIdentificationResponse>(SetupClient(), new JavaScriptSerializer().Serialize(appendOptions(dict)));
        }

        /// <summary>Language
        /// <para>
        /// (POST)Language Endpoint: Returns list of candidate languages.
        /// </para>
        /// </summary>
        /// <param name="file">RosetteFile: RosetteFile Object containing the file (and possibly options) to upload</param>
        /// <returns>LanguageIdentificationResponse containing the results of the request. 
        /// The response is an ordered list of detected languages.
        /// </returns>
        public LanguageIdentificationResponse Language(RosetteFile file) {
            _uri = "language";
            return Process<LanguageIdentificationResponse>(file);
        }

        /// <summary>Morphology
        /// <para>
        /// (POST)Morphology Endpoint: Returns morphological analysis of input.
        /// </para>
        /// </summary>
        /// <param name="content">(string, optional): Input to process (JSON string or base64 encoding of non-JSON string)</param>
        /// <param name="language">(string, optional): Language: ISO 639-3 code (ignored for the /language endpoint)</param>
        /// <param name="contentType">(string, optional): not used at this time</param>
        /// <param name="contentUri">(string, optional): URI to accessible content (content and contentUri are mutually exclusive)</param>
        /// <param name="feature">(string, optional): Description of what morphology feature to request from the Rosette server</param>
        /// <param name="genre">(string, optional): genre to categorize the input data</param>
        /// <returns>MorphologyResponse containing the results of the request. 
        /// The response may include lemmas, part of speech tags, compound word components, and Han readings. 
        /// Support for specific return types depends on language.
        /// </returns>
        public MorphologyResponse Morphology(string content = null, string language = null, string contentType = null, string contentUri = null, MorphologyFeature feature = MorphologyFeature.complete, string genre = null)
        {
            _uri = "morphology/" + feature.MorphologyEndpoint();
            return Process<MorphologyResponse>(content, language, contentType, contentUri, genre);
        }

        /// <summary>Morphology
        /// <para>
        /// (POST)Morphology Endpoint: Returns morphological analysis of input.
        /// </para>
        /// </summary>
        /// <param name="dict">Dictionary&lt;object, object&gt;: Dictionary containing parameters as (key,value) pairs</param>
        /// <param name="feature">(string, optional): Description of what morphology feature to request from the Rosette server</param>
        /// <returns>MorphologyResponse containing the results of the request. 
        /// The response may include lemmas, part of speech tags, compound word components, and Han readings. 
        /// Support for specific return types depends on language.
        /// </returns>
        public MorphologyResponse Morphology(Dictionary<object, object> dict, MorphologyFeature feature = MorphologyFeature.complete)
        {
            _uri = "morphology/" + feature.MorphologyEndpoint();
            return getResponse<MorphologyResponse>(SetupClient(), new JavaScriptSerializer().Serialize(appendOptions(dict)));
        }

        /// <summary>Morphology
        /// <para>
        /// (POST)Morphology Endpoint: Returns morphological analysis of input.
        /// </para>
        /// </summary>
        /// <param name="file">RosetteFile: RosetteFile Object containing the file (and possibly options) to upload</param>
        /// <param name="feature">(string, optional): Description of what morphology feature to request from the Rosette server</param>
        /// <returns>MorphologyResponse containing the results of the request. 
        /// The response may include lemmas, part of speech tags, compound word components, and Han readings. 
        /// Support for specific return types depends on language.
        /// </returns>
        public MorphologyResponse Morphology(RosetteFile file, MorphologyFeature feature = MorphologyFeature.complete) {
            _uri = "morphology/" + feature.MorphologyEndpoint();
            return Process<MorphologyResponse>(file);
        }

        /// <summary>NameSimilarity
        /// <para>
        /// (POST)NameSimilarity Endpoint: Returns the result of matching 2 names.
        /// </para>
        /// </summary>
        /// <param name="n1">Name: First name to be matched</param>
        /// <param name="n2">Name: Second name to be matched</param>
        /// <returns>NameSimilarityResponse containing the results of the request. 
        /// </returns>
        public NameSimilarityResponse NameSimilarity(Name n1, Name n2)
        {
            _uri = "name-similarity";

            Dictionary<object, object> dict = new Dictionary<object, object>(){
                { "name1", n1},
                { "name2", n2}
            };

            return getResponse<NameSimilarityResponse>(SetupClient(), new JavaScriptSerializer().Serialize(appendOptions(dict)));
        }

        /// <summary>MatchedName
        /// <para>
        /// (POST)MatchedName Endpoint: Returns the result of matching 2 names.
        /// </para>
        /// </summary>
        /// <param name="n1">Name: First name to be matched</param>
        /// <param name="n2">Name: Second name to be matched</param>
        /// <returns>NameSimilarityResponse containing the results of the request. 
        /// </returns>
        [Obsolete("Use NameSimilarity")]
        public NameSimilarityResponse MatchedName(Name n1, Name n2)
        {
            return NameSimilarity(n1, n2);
        }

        /// <summary>NameSimilarity
        /// <para>
        /// (POST)NameSimilarity Endpoint: Returns the result of matching 2 names.
        /// </para>
        /// </summary>
        /// <param name="dict">Dictionary&lt;object, object&gt;: Dictionary containing parameters as (key,value) pairs</param>
        /// <returns>NameSimilarityResponse containing the results of the request. 
        /// </returns>
        public NameSimilarityResponse NameSimilarity(Dictionary<object, object> dict) {
            _uri = "name-similarity";
            return getResponse<NameSimilarityResponse>(SetupClient(), new JavaScriptSerializer().Serialize(appendOptions(dict)));
        }

        /// <summary>Ping
        /// (GET)Ping Endpoint: Pings Rosette API for a response indicting that the service is available
        /// </summary>
        /// <returns>PingResponse containing the results of the info GET.
        /// The reponse contains a message and time.
        /// </returns>
        public PingResponse Ping() {
            _uri = "ping";
            return getResponse<PingResponse>(SetupClient());
        }

        /// <summary>TextEmbeddings
        /// <para>
        /// (POST)TextEmbeddings Endpoint: Returns an averaged text vector of the input text.
        /// </para>
        /// </summary>
        /// <param name="content">(string, optional): Input to process (JSON string or base64 encoding of non-JSON string)</param>
        /// <param name="language">(string, optional): Language: ISO 639-3 code (ignored for the /language endpoint)</param>
        /// <param name="contentType">(string, optional): not used at this time</param>
        /// <param name="contentUri">(string, optional): URI to accessible content (content and contentUri are mutually exclusive)</param>
        /// <param name="genre">(string, optional): genre to categorize the input data</param>
        /// <returns>
        /// A TextEmbeddingResponse:
        /// Contains a single vector of floating point numbers for your input, known as a text embedding. 
        /// Among other uses, a text embedding enables you to calculate the similarity between two documents or two words.
        /// The text embedding represents the relationships between words in your document in the semantic space. 
        /// The semantic space is a multilingual network that maps the input based on the words and their context. 
        /// Words with similar meanings have similar contexts, and Rosette maps them close to each other
        /// </returns>
        public TextEmbeddingResponse TextEmbedding(string content = null, string language = null, string contentType = null, string contentUri = null, string genre = null)
        {
            _uri = "text-embedding";
            return Process<TextEmbeddingResponse>(content, language, contentType, contentUri, genre);
        }

        /// <summary>TextEmbeddings
        /// <para>
        /// (POST)TextEmbeddings Endpoint: Returns an averaged text vector of the input text.
        /// </para>
        /// </summary>
        /// <param name="dict">Dictionary&lt;object, object&gt;: Dictionary containing parameters as (key,value) pairs</param>
        /// <returns>
        /// A TextEmbeddingResponse:
        /// Contains a single vector of floating point numbers for your input, known as a text embedding. 
        /// Among other uses, a text embedding enables you to calculate the similarity between two documents or two words.
        /// The text embedding represents the relationships between words in your document in the semantic space. 
        /// The semantic space is a multilingual network that maps the input based on the words and their context. 
        /// Words with similar meanings have similar contexts, and Rosette maps them close to each other
        /// </returns>
        public TextEmbeddingResponse TextEmbedding(Dictionary<object, object> dict)
        {
            _uri = "text-embedding";
            return getResponse<TextEmbeddingResponse>(SetupClient(), new JavaScriptSerializer().Serialize(appendOptions(dict)));
        }

        /// <summary>TextEmbeddings
        /// <para>
        /// (POST)TextEmbeddings Endpoint: Returns an averaged text vector of the input text.
        /// </para>
        /// </summary>
        /// <param name="file">RosetteFile: RosetteFile Object containing the file (and possibly options) to upload</param>
        /// <returns>
        /// A TextEmbeddingResponse:
        /// Contains a single vector of floating point numbers for your input, known as a text embedding. 
        /// Among other uses, a text embedding enables you to calculate the similarity between two documents or two words.
        /// The text embedding represents the relationships between words in your document in the semantic space. 
        /// The semantic space is a multilingual network that maps the input based on the words and their context. 
        /// Words with similar meanings have similar contexts, and Rosette maps them close to each other
        /// </returns>
        public TextEmbeddingResponse TextEmbedding(RosetteFile file)
        {
            _uri = "text-embedding";
            return Process<TextEmbeddingResponse>(file);
        }

        /// <summary>SyntaxDependencies
        /// <para>
        /// (POST)SyntaxDependencies Endpoint: Return the syntactic dependencies of the input text.
        /// </para>
        /// </summary>
        /// <param name="content">(string, optional): Input to process (JSON string or base64 encoding of non-JSON string)</param>
        /// <param name="language">(string, optional): Language: ISO 639-3 code (ignored for the /language endpoint)</param>
        /// <param name="contentType">(string, optional): not used at this time</param>
        /// <param name="contentUri">(string, optional): URI to accessible content (content and contentUri are mutually exclusive)</param>
        /// <param name="genre">(string, optional): genre to categorize the input data</param>
        /// <returns>
        /// A SyntaxDependenciesResponse:
        /// The parsed text is represented in terms of syntactic dependencies 
        /// </returns>
        public SyntaxDependenciesResponse SyntaxDependencies(string content = null, string language = null, string contentType = null, string contentUri = null, string genre = null)
        {
            _uri = "syntax/dependencies";
            return Process<SyntaxDependenciesResponse>(content, language, contentType, contentUri, genre);
        }

        /// <summary>SyntaxDependencies
        /// <para>
        /// (POST)SyntaxDependencies Endpoint: Return the syntactic dependencies of the input text.
        /// </para>
        /// </summary>
        /// <param name="dict">Dictionary&lt;object, object&gt;: Dictionary containing parameters as (key,value) pairs</param>
        /// <returns>
        /// A SyntaxDependenciesResponse:
        /// The parsed text is represented in terms of syntactic dependencies 
        /// </returns>
        public SyntaxDependenciesResponse SyntaxDependencies(Dictionary<object, object> dict)
        {
            _uri = "syntax/dependencies";
            return getResponse<SyntaxDependenciesResponse>(SetupClient(), new JavaScriptSerializer().Serialize(appendOptions(dict)));
        }

        /// <summary>SyntaxDependencies
        /// <para>
        /// (POST)SyntaxDependencies Endpoint: Return the syntactic dependencies of the input text.
        /// </para>
        /// </summary>
        /// <param name="file">RosetteFile: RosetteFile Object containing the file (and possibly options) to upload</param>
        /// <returns>
        /// A SyntaxDependenciesResponse:
        /// The parsed text is represented in terms of syntactic dependencies 
        /// </returns>
        public SyntaxDependenciesResponse SyntaxDependencies(RosetteFile file)
        {
            _uri = "syntax/dependencies";
            return Process<SyntaxDependenciesResponse>(file);
        }

        /// <summary>Relationships
        /// <para>
        /// (POST)Relationships Endpoint: Returns each relationship extracted from the input.
        /// </para>
        /// </summary>
        /// <param name="content">(string, optional): Input to process (JSON string or base64 encoding of non-JSON string)</param>
        /// <param name="language">(string, optional): Language: ISO 639-3 code (ignored for the /language endpoint)</param>
        /// <param name="contentType">(string, optional): not used at this time</param>
        /// <param name="contentUri">(string, optional): URI to accessible content (content and contentUri are mutually exclusive)</param>
        /// <param name="genre">(string, optional): genre to categorize the input data</param>
        /// <returns>
        /// RosetteResponse of extracted relationships. A relationship contains
        /// 
        /// predicate - usually the main verb, property or action that is expressed by the text
        /// arg1 - usually the subject, agent or main actor of the relationship
        /// arg2 [optional] - complements the predicate and is usually the object, theme or patient of the relationship
        /// arg3 [optional] - usually an additional object in ditransitive verbs
        /// adjuncts [optional] - contain all optional parts of a relationship which are not temporal or locative expressions
        /// locatives [optional] - usually express the locations the action expressed by the relationship took place
        /// temporals [optional] - usually express the time in which the action expressed by the relationship took place
        /// </returns>
        public RelationshipsResponse Relationships(string content = null, string language = null, string contentType = null, string contentUri = null, string genre = null)
        {
            _uri = "relationships";
            return Process<RelationshipsResponse>(content, language, contentType, contentUri, genre);
        }

        /// <summary>Relationships
        /// <para>
        /// (POST)Relationships Endpoint: Returns each relationship extracted from the input.
        /// </para>
        /// </summary>
        /// <param name="dict">Dictionary&lt;object, object&gt;: Dictionary containing parameters as (key,value) pairs</param>
        /// <returns>
        /// RelationshipsResponse of extracted relationships. A relationship contains
        /// 
        /// predicate - usually the main verb, property or action that is expressed by the text
        /// arg1 - usually the subject, agent or main actor of the relationship
        /// arg2 [optional] - complements the predicate and is usually the object, theme or patient of the relationship
        /// arg3 [optional] - usually an additional object in ditransitive verbs
        /// adjuncts [optional] - contain all optional parts of a relationship which are not temporal or locative expressions
        /// locatives [optional] - usually express the locations the action expressed by the relationship took place
        /// temporals [optional] - usually express the time in which the action expressed by the relationship took place
        /// </returns>
        public RelationshipsResponse Relationships(Dictionary<object, object> dict)
        {
            _uri = "relationships";
            return getResponse<RelationshipsResponse>(SetupClient(), new JavaScriptSerializer().Serialize(appendOptions(dict)));
        }

        /// <summary>Relationships
        /// <para>
        /// (POST)Relationships Endpoint: Returns each relationship extracted from the input.
        /// </para>
        /// </summary>
        /// <param name="file">RosetteFile: RosetteFile Object containing the file (and possibly options) to upload</param>
        /// <returns>
        /// RelationshipsResponse of extracted relationships. A relationship contains
        /// 
        /// predicate - usually the main verb, property or action that is expressed by the text
        /// arg1 - usually the subject, agent or main actor of the relationship
        /// arg2 [optional] - complements the predicate and is usually the object, theme or patient of the relationship
        /// arg3 [optional] - usually an additional object in ditransitive verbs
        /// adjuncts [optional] - contain all optional parts of a relationship which are not temporal or locative expressions
        /// locatives [optional] - usually express the locations the action expressed by the relationship took place
        /// temporals [optional] - usually express the time in which the action expressed by the relationship took place
        /// </returns>
        public RelationshipsResponse Relationships(RosetteFile file) {
            _uri = "relationships";
            return Process<RelationshipsResponse>(file);
        }

        /// <summary>Sentences
        /// <para>
        /// (POST)Sentences Endpoint: Divides the input into sentences.
        /// </para>
        /// </summary>
        /// <param name="content">(string, optional): Input to process (JSON string or base64 encoding of non-JSON string)</param>
        /// <param name="language">(string, optional): Language: ISO 639-3 code (ignored for the /language endpoint)</param>
        /// <param name="contentType">(string, optional): not used at this time</param>
        /// <param name="contentUri">(string, optional): URI to accessible content (content and contentUri are mutually exclusive)</param>
        /// <param name="genre">(string, optional): genre to categorize the input data</param>
        /// <returns>SentenceTaggingResponse containing the results of the request 
        /// The response contains a list of sentences.
        /// </returns>
        public SentenceTaggingResponse Sentences(string content = null, string language = null, string contentType = null, string contentUri = null, string genre = null)
        {
            _uri = "sentences";
            return Process<SentenceTaggingResponse>(content, language, contentType, contentUri, genre);
        }

        /// <summary>Sentences
        /// <para>
        /// (POST)Sentences Endpoint: Divides the input into sentences.
        /// </para>
        /// </summary>
        /// <param name="dict">Dictionary&lt;object, object&gt;: Dictionary containing parameters as (key,value) pairs</param>
        /// <returns>SentenceTaggingResponse containing the results of the request. 
        /// The response contains a list of sentences.
        /// </returns>
        public SentenceTaggingResponse Sentences(Dictionary<object, object> dict)
        {
            _uri = "sentences";
            return getResponse<SentenceTaggingResponse>(SetupClient(), new JavaScriptSerializer().Serialize(appendOptions(dict)));
        }

        /// <summary>Sentences
        /// <para>
        /// (POST)Sentences Endpoint: Divides the input into sentences.
        /// </para>
        /// </summary>
        /// <param name="file">RosetteFile: RosetteFile Object containing the file (and possibly options) to upload</param>
        /// <returns>SentenceTaggingResponse containing the results of the request. 
        /// The response contains a list of sentences.
        /// </returns>
        public SentenceTaggingResponse Sentences(RosetteFile file) {
            _uri = "sentences";
            return Process<SentenceTaggingResponse>(file);
        }

        /// <summary>Sentiment
        /// <para>
        /// (POST)Sentiment Endpoint: Analyzes the positive and negative sentiment expressed by the input.
        /// </para>
        /// </summary>
        /// <param name="content">(string, optional): Input to process (JSON string or base64 encoding of non-JSON string)</param>
        /// <param name="language">(string, optional): Language: ISO 639-3 code (ignored for the /language endpoint)</param>
        /// <param name="contentType">(string, optional): not used at this time</param>
        /// <param name="contentUri">(string, optional): URI to accessible content (content and contentUri are mutually exclusive)</param>
        /// <param name="genre">(string, optional): genre to categorize the input data</param>
        /// <returns>SentimentResponse containing the results of the request. 
        /// The response contains sentiment analysis results.
        /// </returns>
        public SentimentResponse Sentiment(string content = null, string language = null, string contentType = null, string contentUri = null, string genre = null)
        {
            _uri = "sentiment";
            return Process<SentimentResponse>(content, language, contentType, contentUri, genre);
        }

        /// <summary>Sentiment
        /// <para>
        /// (POST)Sentiment Endpoint: Analyzes the positive and negative sentiment expressed by the input.
        /// </para>
        /// </summary>
        /// <param name="dict">Dictionary&lt;object, object&gt;: Dictionary containing parameters as (key,value) pairs</param>
        /// <returns>SentimentResponse containing the results of the request. 
        /// The response contains sentiment analysis results.
        /// </returns>
        public SentimentResponse Sentiment(Dictionary<object, object> dict)
        {
            _uri = "sentiment";
            return getResponse<SentimentResponse>(SetupClient(), new JavaScriptSerializer().Serialize(appendOptions(dict)));
        }

        /// <summary>Sentiment
        /// <para>
        /// (POST)Sentiment Endpoint: Analyzes the positive and negative sentiment expressed by the input.
        /// </para>
        /// </summary>
        /// <param name="file">RosetteFile: RosetteFile Object containing the file (and possibly options) to upload</param>
        /// <returns>SentimentResponse containing the results of the request. 
        /// The response contains sentiment analysis results.
        /// </returns>
        public SentimentResponse Sentiment(RosetteFile file) {
            _uri = "sentiment";
            return Process<SentimentResponse>(file);
        }

        /// <summary>Tokens
        /// <para>
        /// (POST)Tokens Endpoint: Divides the input into tokens.
        /// </para>
        /// </summary>
        /// <param name="content">(string, optional): Input to process (JSON string or base64 encoding of non-JSON string)</param>
        /// <param name="language">(string, optional): Language: ISO 639-3 code (ignored for the /language endpoint)</param>
        /// <param name="contentType">(string, optional): not used at this time</param>
        /// <param name="contentUri">(string, optional): URI to accessible content (content and contentUri are mutually exclusive)</param>
        /// <param name="genre">(string, optional): genre to categorize the input data</param>
        /// <returns>TokenizationResponse containing the results of the request. 
        /// The response contains a list of tokens.
        /// </returns>
        public TokenizationResponse Tokens(string content = null, string language = null, string contentType = null, string contentUri = null, string genre = null)
        {
            _uri = "tokens";
            return Process<TokenizationResponse>(content, language, contentType, contentUri, genre);
        }

        /// <summary>Tokens
        /// <para>
        /// (POST)Tokens Endpoint: Divides the input into tokens.
        /// </para>
        /// </summary>
        /// <param name="dict">Dictionary&lt;object, object&gt;: Dictionary containing parameters as (key,value) pairs</param>
        /// <returns>TokenizationResponse containing the results of the request. 
        /// The response contains a list of tokens.
        /// </returns>
        public TokenizationResponse Tokens(Dictionary<object, object> dict) {
            _uri = "tokens";
            return getResponse<TokenizationResponse>(SetupClient(), new JavaScriptSerializer().Serialize(appendOptions(dict)));
        }

        /// <summary>Tokens
        /// <para>
        /// (POST)Tokens Endpoint: Divides the input into tokens.
        /// </para>
        /// </summary>
        /// <param name="file">RosetteFile: RosetteFile Object containing the file (and possibly options) to upload</param>
        /// <returns>TokenizationResponse containing the results of the request. 
        /// The response contains a list of tokens.
        /// </returns>
        public TokenizationResponse Tokens(RosetteFile file) {
            _uri = "tokens";
            return Process<TokenizationResponse>(file);
        }

        /// <summary>NameTranslation
        /// <para>
        /// (POST)NameTranslation Endpoint: Returns the translation of a name. You must specify the name to translate and the target language for the translation.
        /// </para>
        /// </summary>
        /// <param name="name">string: Name to be translated</param>
        /// <param name="sourceLanguageOfUse">(string, optional): ISO 639-3 code for the name's language of use</param>
        /// <param name="sourceScript">(string, optional): ISO 15924 code for the name's script</param>
        /// <param name="targetLanguage">(string): ISO 639-3 code for the translation language</param>
        /// <param name="targetScript">(string, optional): ISO 15924 code for the translation script</param>
        /// <param name="targetScheme">(string, optional): transliteration scheme for the translation</param>
        /// <param name="sourceLanguageOfOrigin">(string, optional): ISO 639-3 code for the name's language of origin</param>
        /// <param name="entityType">(string, optional): Entity type of the name: PERSON, LOCATION, or ORGANIZATION</param>
        /// <param name="genre">(string, optional): genre to categorize the input data</param>
        /// <returns>TranslateNamesResponse containing the results of the request. 
        /// </returns>
        public TranslateNamesResponse NameTranslation(string name, string sourceLanguageOfUse = null, string sourceScript = null, string targetLanguage = null, string targetScript = null, string targetScheme = null, string sourceLanguageOfOrigin = null, string entityType = null, string genre = null) {
            _uri = "name-translation";

            Dictionary<object, object> dict = new Dictionary<object, object>(){
                { "name", name},
                { "sourceLanguageOfUse", sourceLanguageOfUse},
                { "sourceScript", sourceScript},
                { "targetLanguage", targetLanguage},
                { "targetScript", targetScript},
                { "targetScheme", targetScheme},
                { "sourceLanguageOfOrigin", sourceLanguageOfOrigin},
                { "entityType", entityType},
                { "genre", genre}
            }.Where(f => f.Value != null).ToDictionary(x => x.Key, x => x.Value);

            return getResponse<TranslateNamesResponse>(SetupClient(), new JavaScriptSerializer().Serialize(appendOptions(dict)));
        }

        /// <summary>NameTranslation
        /// <para>
        /// (POST)NameTranslation Endpoint: Returns the translation of a name. You must specify the name to translate and the target language for the translation.
        /// </para>
        /// </summary>
        /// <param name="dict">Dictionary&lt;object, object&gt;: Dictionary containing parameters as (key,value) pairs</param>
        /// <returns>TranslateNamesResponse containing the results of the request. </returns>
        public TranslateNamesResponse NameTranslation(Dictionary<object, object> dict) {
            _uri = "name-translation";
            return getResponse<TranslateNamesResponse>(SetupClient(), new JavaScriptSerializer().Serialize(appendOptions(dict)));
        }

        /// <summary>getResponse
        /// <para>
        /// getResponse: Internal function to get the response from the Rosette API server using the request
        /// </para>
        /// </summary>
        /// <param name="client">HttpClient: Client to use to access the Rosette server.</param>
        /// <param name="jsonRequest">(string, optional): Content to use as the request to the server with POST. If none given, assume an Info endpoint and use GET</param>
        /// <param name="multiPart">(MultipartFormDataContent, optional): Used for file uploads</param>
        /// <returns>RosetteResponse derivative</returns>
        private T getResponse<T>(HttpClient client, string jsonRequest = null, MultipartFormDataContent multiPart = null) where T : RosetteResponse {
            HttpResponseMessage responseMsg = null;
            if (client != null) {
                string wholeURI = _uri;
                if (wholeURI.StartsWith("/")) {
                    wholeURI = wholeURI.Substring(1);
                }

                if (jsonRequest != null)
                {
                    HttpContent content = new StringContent(jsonRequest);
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                    Task<HttpResponseMessage> task = Task.Run<HttpResponseMessage>(async () => await client.PostAsync(wholeURI, content));
                    responseMsg = task.Result;
                }
                else if (multiPart != null)
                {
                    Task<HttpResponseMessage> task = Task.Run<HttpResponseMessage>(async () => await client.PostAsync(wholeURI, multiPart));
                    responseMsg = task.Result;
                }
                else
                {
                    Task<HttpResponseMessage> task = Task.Run<HttpResponseMessage>(async () => await client.GetAsync(wholeURI));
                    responseMsg = task.Result;
                }
            }
            if (responseMsg == null)
            {
                throw new RosetteException(client == null ? "Client not initialized." : "The server returned a null response.");
            }
            if (responseMsg.IsSuccessStatusCode)
            {
                T response = (T)Activator.CreateInstance(typeof(T), new object[] { responseMsg });
                this.SetCallConcurrency(response);
                return response;
            }
            else
            {
                throw new RosetteException(string.Format("{0}: {1}", responseMsg.ReasonPhrase, RosetteResponse.contentToString(responseMsg.Content)), (int)responseMsg.StatusCode);
            }
        }

        /// <summary>
        /// Sets the call concurrency limit if it has not already been set after API initialization.
        /// </summary>
        /// <param name="response"></param>
        private void SetCallConcurrency(RosetteResponse response)
        {
            if (response.Headers.ContainsKey(CONCURRENCY_HEADER))
            {
                int callConcurrency = System.Net.ServicePointManager.DefaultConnectionLimit;
                bool success = Int32.TryParse(response.Headers[CONCURRENCY_HEADER], out callConcurrency);
                if (success && callConcurrency != System.Net.ServicePointManager.DefaultConnectionLimit && callConcurrency > 1)
                {
                    System.Net.ServicePointManager.DefaultConnectionLimit = callConcurrency;
                    this.Concurrency = callConcurrency;
                }
            }
        }

        /// <summary>Process
        /// <para>
        /// Process: Internal function to convert a RosetteFile into a dictionary to use for getResponse
        /// </para>
        /// </summary>
        /// <param name="file">RosetteFile: File being uploaded to use as a request to the Rosette server.</param>
        /// <returns>RosetteResponse derivative containing the results of the response from the server from the getResponse call.</returns>
        private T Process<T>(RosetteFile file) where T : RosetteResponse {
            return getResponse<T>(SetupClient(), null, file.AsMultipart());
        }

        /// <summary>Process
        /// <para>
        /// Process: Internal function to convert a RosetteFile into a dictionary to use for getResponse
        /// </para>
        /// </summary>
        /// <param name="content">(string, optional): Input to process (JSON string or base64 encoding of non-JSON string)</param>
        /// <param name="language">(string, optional): Language: ISO 639-3 code (ignored for the /language endpoint)</param>
        /// <param name="contentType">(string, optional): not used at this time</param>
        /// <param name="contentUri">(string, optional): URI to accessible content (content and contentUri are mutually exclusive)</param>
        /// <param name="genre">(string, optional): genre to categorize the input data</param>
        /// <returns>RosetteResponse derivative containing the results of the response from the server from the getResponse call.</returns>
        private T Process<T>(string content = null, string language = null, string contentType = null, string contentUri = null, string genre = null) where T : RosetteResponse {
            if (content == null) {
                if (contentUri == null) {
                    throw new RosetteException("Must supply one of Content or ContentUri", -3);
                }
            }
            else {
                if (contentUri != null) {
                    throw new RosetteException("Cannot supply both Content and ContentUri", -3);
                }
            }

            Dictionary<object, object> dict = new Dictionary<object, object>(){
                { "language", language},
                { "content", content},
                { "contentUri", contentUri},
                { "genre", genre}
            }.Where(f => f.Value != null).ToDictionary(x => x.Key, x => x.Value);

            return getResponse<T>(SetupClient(), new JavaScriptSerializer().Serialize(appendOptions(dict)));
        }

        /// <summary>SetupClient
        /// <para>
        /// SetupClient: Internal function to setup the HttpClient
        /// Uses the Client if one has been set. Otherwise create a new one. 
        /// </para>
        /// </summary>
        /// <returns>HttpClient client to use to access the Rosette server.</returns>
        private HttpClient SetupClient() {
            HttpClient client;
            if (!URIstring.EndsWith("/")) {
                URIstring = URIstring + "/";
            }

            if (Client == null) {
                client =
                    new HttpClient(
                        new HttpClientHandler {
                            AutomaticDecompression = DecompressionMethods.GZip
                                                     | DecompressionMethods.Deflate
                        });
                client.BaseAddress = new Uri(URIstring);
                if (Timeout != 0) {
                    client.Timeout = TimeSpan.FromMilliseconds(Timeout);
                }
            }
            else {
                client = Client;
                if (client.BaseAddress == null) {
                    client.BaseAddress = new Uri(URIstring);
                }
                if (client.Timeout == TimeSpan.Zero) {
                    client.Timeout = new TimeSpan(0, 0, Timeout);
                }
            }
            try {
                client.DefaultRequestHeaders.Clear();
            }
            catch {
                // exception can be ignored
            }
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            if (UserKey != null) {
                client.DefaultRequestHeaders.Add("X-RosetteAPI-Key", UserKey);
            }
            if (Debug) {
                client.DefaultRequestHeaders.Add("X-RosetteAPI-Devel", "true");
            }

            Regex pattern = new Regex("^X-RosetteAPI-");
            if(_customHeaders.Count > 0) {
                foreach(KeyValuePair<string, string> entry in _customHeaders) {
                    Match match = pattern.Match(entry.Key);
                    if(match.Success) {
                        client.DefaultRequestHeaders.Add(entry.Key, entry.Value);
                    } else {
                        throw new RosetteException("Custom header name must begin with \"X-RosetteAPI-\"");
                    }

                }
            }
            client.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("gzip"));
            client.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("deflate"));
            client.DefaultRequestHeaders.Add("User-Agent", "RosetteAPICsharp/" + Version);
            client.DefaultRequestHeaders.Add("X-RosetteAPI-Binding", "csharp");
            client.DefaultRequestHeaders.Add("X-RosetteAPI-Binding-Version", Version);

            return client;
        }

        /// <summary>Decompress
        /// <para>Method to decompress GZIP files
        /// Source: http://www.dotnetperls.com/decompress
        /// </para>
        /// </summary>
        /// <param name="gzip">(byte[]): Data in byte form to decompress</param>
        /// <returns>(byte[]) Decompressed data</returns>
        private static byte[] Decompress(byte[] gzip) {
            // Create a GZIP stream with decompression mode.
            // ... Then create a buffer and write into while reading from the GZIP stream.
            using (GZipStream stream = new GZipStream(new MemoryStream(gzip), CompressionMode.Decompress)) {
                const int size = 4096;
                byte[] buffer = new byte[size];
                using (MemoryStream memory = new MemoryStream()) {
                    int count = 0;
                    do {
                        count = stream.Read(buffer, 0, size);
                        if (count > 0) {
                            memory.Write(buffer, 0, count);
                        }
                    }
                    while (count > 0);
                    return memory.ToArray();
                }
            }
        }
    }
}




