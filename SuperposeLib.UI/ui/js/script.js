﻿var app = angular.module("tutorialWebApp", [
    "ngRoute"
]);

app.config([
    "$routeProvider", function($routeProvider) {
        $routeProvider
            // Home
            .when("/", {
                templateUrl: "partials/home.html" /*,controller: ""*/
            })
            // else 404
            .otherwise("/404", { templateUrl: "partials/404.html", controller: "PageCtrl" });
    }
]);


angular.module("tutorialWebApp").controller("ActorsCtrl", function($scope, $rootScope, $http, $q, $timeout, $interval) {
    var endpoint = "/api/values/ActorSystemStates";
    var getThings = function(selection) {
        var deferred = $q.defer();
        $http.get(endpoint, JSON.stringify(selection), { headers: { 'Content-Type': "application/json" } }).success(deferred.resolve).error(deferred.reject);

        return deferred.promise;
    };

    $scope.statictics = {};

    $scope.stringify = function(data) {
        return JSON.stringify(data);
    };
    $scope.actors = [];


    $scope.actorState = { state: {} };

    $.connection.hub.url = "http://localhost:8008/signalr";
    var chat = $.connection.myHub;
    chat.client.processing = function(response) {
        $timeout(function() {
            $scope.jobExecuting = response;
            $timeout(function () {
                $scope.jobExecuting = "";
            }, 1000);
        });
    };

    chat.client.jobStatisticsCompleted = function(response) {
        $timeout(function() {
            $scope.statictics = response || {
                TotalDeletedJobs: 0,
                TotalFailedJobs: 0,
                TotalNumberOfJobs: 0,
                TotalProcessingJobs: 0,
                TotalQueuedJobs: 0,
                TotalSuccessfullJobs: 0,
                TotalUnknownJobs: 0
            };
        });
    };
    // Start the connection.
    $.connection.hub.start().done(function() {
        chat.server.getJobStatistics();
        chat.server.getCurrentQueue();
    });
    $scope.jobQueue = {};
    $scope.queueSampleJob = function() {
        chat.server.queueSampleJob();
    };

    chat.client.currentQueue = function(response) {
        $timeout(function() {
            $scope.jobQueue = response;
          
        });
    };
    $scope.getCurrentQueue = function() {
        chat.server.getCurrentQueue();
    };

    $scope.setQueueMaxNumberOfJobsPerLoad = function() {
        chat.server.setQueueMaxNumberOfJobsPerLoad($scope.jobQueue.MaxNumberOfJobsPerLoad);
    };
    $scope.setQueueStorgePollSecondsInterval = function() {
        chat.server.setQueueStorgePollSecondsInterval($scope.jobQueue.StorgePollSecondsInterval);
    };
    $scope.setQueueWorkerPoolCount = function() {
        chat.server.setQueueWorkerPoolCount($scope.jobQueue.WorkerPoolCount);
    };
    $scope.updateCurrentQueue = function() {
        $scope.setQueueWorkerPoolCount();
        $scope.setQueueStorgePollSecondsInterval();
        $scope.setQueueMaxNumberOfJobsPerLoad();
    };


    chat.client.jobsList = function (response) {
        $timeout(function () {
            $scope.jobList = response;
        });
    };

    $scope.getJobsByJobStateType = function (stateType, take, keep, queue) {
        chat.server.getJobsByJobStateType(stateType, take, keep, queue);
    };


});