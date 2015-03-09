angular.module('app', ['ngResource'])
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
    function(Account, $scope) {
      Account.get(function(data) {
        console.log(data);
      });
    }
  ]);