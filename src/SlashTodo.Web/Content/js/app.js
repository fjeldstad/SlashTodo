angular.module('app', ['ngResource', 'ngAnimate'])
  .factory('_', [
    '$window',
    function($window) {
      return $window._;
    }
  ])
  .factory('Account', [
    'BaseUrl',
    '$resource',
    function(BaseUrl, $resource) {
      return $resource(BaseUrl + '/account');
    }
  ])
  .controller('DashboardController', [
    'Account',
    '$scope',
    '_',
    '$http',
    'BaseUrl',
    '$timeout',
    function(Account, $scope, _, $http, BaseUrl, $timeout) {
      $scope.setupComplete = false;
      $scope.waitingForServer = true;
      $scope.pendingUpdateSlashCommandToken = false;
      $scope.pendingUpdateIncomingWebhookUrl = false;
      $scope.settings = {
        slashCommandToken: null,
        incomingWebhookUrl: null
      };

      $scope.updateSetupComplete = function() {
        $scope.setupComplete =
          $scope.settings.slashCommandToken &&
          $scope.settings.incomingWebhookUrl;
      };

      $scope.setupWatches = function() {
        $scope.$watch('settings.slashCommandToken', function(newValue, oldValue) {
          newValue = newValue || null;
          oldValue = oldValue || null;
          if (newValue !== oldValue) {
            $scope.pendingUpdateSlashCommandToken = true;
            $http.post(BaseUrl + '/account/slash-command-token', {
              slashCommandToken: newValue || null
            }).success(function(data) {
              $scope.updateSlashCommandTokenSuccess = true;
              $timeout(function() {
                $scope.updateSlashCommandTokenSuccess = false;
              }, 2000);
            }).error(function(data, status) {
              console.log(status);
            }).finally(function() {
              $scope.updateSetupComplete();
              $scope.pendingUpdateSlashCommandToken = false;
            });
          }
        });

        $scope.$watch('settings.incomingWebhookUrl', function(newValue, oldValue) {
          newValue = newValue || null;
          oldValue = oldValue || null;
          if (newValue !== oldValue) {
            $scope.pendingUpdateIncomingWebhookUrl = true;
            $http.post(BaseUrl + '/account/incoming-webhook-url', {
              incomingWebhookUrl: newValue || null
            }).success(function(data) {
              $scope.updateIncomingWebhookUrlSuccess = true;
              $timeout(function() {
                $scope.updateIncomingWebhookUrlSuccess = false;
              }, 2000);
              $scope.incomingWebhookUrlInvalid = false;
            }).error(function(data, status) {
              $scope.incomingWebhookUrlInvalid = true;
            }).finally(function() {
              $scope.updateSetupComplete();
              $scope.pendingUpdateIncomingWebhookUrl = false;
            });
          }
        });
      };

      $scope.settings = Account.get();
      $scope.settings.$promise
        .finally(function() {
          $scope.updateSetupComplete();
          $scope.waitingForServer = false;
          $timeout($scope.setupWatches, 0);
        });
    }
  ]);