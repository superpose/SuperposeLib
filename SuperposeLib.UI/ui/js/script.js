﻿var app = angular.module('tutorialWebApp', [
      'ngRoute'
]);

app.config([
    '$routeProvider', function ($routeProvider) {
        $routeProvider
            // Home
            .when("/", { templateUrl: "partials/home.html", controller: "ActorsCtrl" })
           // else 404
            .otherwise("/404", { templateUrl: "partials/404.html", controller: "PageCtrl" });
    }
]);




angular.module('tutorialWebApp').controller('ActorsCtrl', function ($scope, $rootScope, $http, $q, $timeout, $interval, Grapgher) {
    var endpoint = '/api/values/ActorSystemStates';
    var getThings = function (selection) {
        var deferred = $q.defer();
        $http.get(endpoint, JSON.stringify(selection), { headers: { 'Content-Type': 'application/json' } }).success(deferred.resolve).error(deferred.reject);

        return deferred.promise;
    };
    $scope.stringify = function (data) {
        return JSON.stringify(data);
    };
    $scope.actors = [];
   

    $scope.actorState = { state: {} };

    $.connection.hub.url = "http://localhost:8008/signalr";
    var chat = $.connection.myHub;
    chat.client.addMessage = function ( response) {
        $timeout(function() {
           alert(response);
        });
    };
    // Start the connection.
    $.connection.hub.start().done(function () {
        // getActorSystemStates();
    });

});

