﻿import React, { Component } from "react";
import { NavLink } from "react-router-dom";
import { SessionService } from './services';
import { withRouter, Link } from 'react-router-dom';
import { translate } from 'react-i18next';
import { EndpointService } from './services';
import Constants from './constants';
import AppDispatcher from './appDispatcher';

import { IconButton, RaisedButton, Drawer, SelectField, MenuItem } from 'material-ui';
import { List, ListItem } from 'material-ui/List';

class Layout extends Component {
    constructor(props) {
        super(props);
        this._appDispatcher = null;
        this._sessionFrame = null;
        this._checkSessionInterval = null;
        this.disconnect = this.disconnect.bind(this);
        this.toggleValue = this.toggleValue.bind(this);
        this.navigate = this.navigate.bind(this);
        this.refresh = this.refresh.bind(this);
        this.startCheckSession = this.startCheckSession.bind(this);
        this.stopCheckSession = this.stopCheckSession.bind(this);
        this.state = {
            isLoggedIn: false,
            isOauthDisplayed: false,
            isScimDisplayed: false,
            isAuthDisplayed: false,
            isDrawerDisplayed: false,
            openidEndpoints: [],
            authEndpoints: [],
            scimEndpoints: []
        };
    }
    /**
     * Disconnect the user.
     * @param {any} e
     */
    disconnect() {
        // TODO : Resolve this url.
        var url = "http://localhost:60000/end_session?post_logout_redirect_uri=http://localhost:64950/end_session&id_token_hint="+ SessionService.getSession().id_token;
        var w = window.open(url, 'targetWindow', 'toolbar=no,location=no,status=no,menubar=no,scrollbars=yes,resizable=yes,width=400,height=400');
        var interval = setInterval(function() {
            if (w.closed) {
                clearInterval(interval);
                return;
            }

            var href = w.location.href;
            if (href === "http://localhost:64950/end_session") {                
                clearInterval(interval);
                w.close();
            }
        });
    }

    toggleValue(menu) {    
        this.setState({
            [menu]: !this.state[menu]
        });
    }

    navigate(href) {
        this.props.history.push(href);
    }

    /**
    * Refresh the settings menu.
    */
    refresh() {
        var self = this;
        EndpointService.getAll().then(function(endpoints) {
            self.setState({
                authEndpoints: endpoints.filter(function(endpoint) { return endpoint.type === 0; }),
                openidEndpoints: endpoints.filter(function(endpoint) { return endpoint.type === 1; }),
                scimEndpoints: endpoints.filter(function(endpoint) { return endpoint.type === 2; })
            });
        }).catch(function() {

        });
    }

    startCheckSession() {
        var self = this;
        if (self._checkSessionInterval) {
            return;
        }

        var evt = window.addEventListener("message", function(e) {
            if (e.data !== 'unchanged') {
                SessionService.remove();
                console.log(e.data);
                self.setState({
                    isLoggedIn: false
                });
                AppDispatcher.dispatch({
                    actionName: Constants.events.USER_LOGGED_OUT
                });
                self.props.history.push('/');
            }
        }, false);
        var originUrl = window.location.protocol + "//" + window.location.host;
        self._checkSessionInterval = setInterval(function() { 
            var session = SessionService.getSession();
            var message = "ResourceManagerClientId ";
            if (session) {
                message += session.sessionState;
            } else {
                session += "tmp";
            }

            var win = self._sessionFrame.contentWindow;
            // TODO : Externalize the client_id & openid url.
            win.postMessage(message, "http://localhost:60000");
        }, 3*1000);
    }

    stopCheckSession() {
        var self = this;
        if (!self._checkSessionInterval) {
            return;
        }

        clearInterval(self._checkSessionInterval);
        self._checkSessionInterval = null;
    }

    render() {
        var self = this;
        const { t } = this.props;
        var openidEndpoints = [];
        var authEndpoints = [];
        var scimEndpoints = [];
        if (self.state.openidEndpoints) {
            self.state.openidEndpoints.forEach(function(openidEndpoint) {
                openidEndpoints.push((<MenuItem key={openidEndpoint.name} primaryText={openidEndpoint.description}></MenuItem>));
            });
        }

        if (self.state.authEndpoints) {
            self.state.authEndpoints.forEach(function(authEndpoint) {
                authEndpoints.push((<MenuItem key={authEndpoint.name} primaryText={authEndpoint.description}></MenuItem>));
            });
        }

        if (self.state.scimEndpoints) {
            self.state.scimEndpoints.forEach(function(scimEndpoint) {
                scimEndpoints.push((<MenuItem key={scimEndpoint.name} primaryText={scimEndpoint.description}></MenuItem>))
            });
        }

        return (<div>
            <Drawer width={300} docked={false} onRequestChange={(open) => self.setState({
                isDrawerDisplayed: open
            })} openSecondary={true} open={this.state.isDrawerDisplayed}>
                <div style={{padding: "20px"}}>
                    <ul className="nav nav-tabs">
                        <li className="nav-item"><a href="#" className="nav-link">{t('yourPreferences')}</a></li>
                    </ul>
                    <div>
                        <div className="form-group">
                            <span>{t('selectedPreferredOpenIdProvider')}</span>
                            <SelectField>
                                {openidEndpoints}
                            </SelectField>
                        </div>
                        <div className="form-group">
                            <span>{t('selectPreferredAuthorizationServer')}</span>
                            <SelectField>
                                {authEndpoints}
                            </SelectField>
                        </div>
                        <div className="form-group">
                            <span>{t('selectPreferredScimServer')}</span>
                            <SelectField>
                                {scimEndpoints}
                            </SelectField>
                        </div>
                        <RaisedButton label={t('saveChanges')} primary={true} />
                    </div>
                </div>
            </Drawer>
            <nav className="navbar-static-side navbar left">
                <div className="sidebar-collapse">
                    <List>
                        {/* About menu item */}
                        <ListItem primaryText={t('aboutMenuItem')} onClick={() => self.navigate('/about')} />
                        {/* Openid menu item */}
                        {(this.state.isLoggedIn && !process.env.IS_MANAGE_DISABLED && (
                            <ListItem primaryText={t('manageOpenidServers')} nestedItems={[
                                <ListItem primaryText={t('resourceOwners')} onClick={() => self.navigate('/resourceowners')} />, 
                                <ListItem primaryText={t('openidClients')} onClick={() => self.navigate('/openidclients')} />, 
                                <ListItem primaryText={t('openidScopes')} onClick={() => self.navigate('/openidscopes')} />
                            ]} />
                        ))}
                        {/* Authorisation server */}
                        {(this.state.isLoggedIn && !process.env.IS_MANAGE_DISABLED && (
                            <ListItem primaryText={t('manageAuthServers')} nestedItems={[
                                <ListItem primaryText={t('oauthClients')} onClick={() => self.navigate('/authclients')} />,    
                                <ListItem primaryText={t('oauthScopes')} onClick={() => self.navigate('/authscopes')} />,    
                                <ListItem primaryText={t('resources')} onClick={() => self.navigate('/resources')} />            
                            ]} />
                        ))}
                        {/* SCIM server */}
                        {(this.state.isLoggedIn && !process.env.IS_MANAGE_DISABLED && (
                            <ListItem primaryText={t('manageScimServers')} nestedItems={[
                                <ListItem primaryText={t('scimSchemas')} />,
                                <ListItem primaryText={t('scimResources')} />
                            ]} />
                         ))}
                        {/* Logs */}         
                        {!process.env.IS_LOG_DISABLED && this.state.isLoggedIn  && (
                            <ListItem primaryText={t('logsMenuItem')} onClick={() => self.navigate('/logs')} />
                        )}
                        {/* Connect or disconnect */}
                        {(this.state.isLoggedIn ? (
                            <ListItem primaryText={t('disconnectMenuItem')} onClick={() => self.disconnect()} />
                        ) : (
                            <ListItem primaryText={t('connectMenuItem')} onClick={() => self.navigate('/login')} />
                        ))}
                    </List>
                </div>
            </nav>
            <section id="wrapper">
                { /* Navigation */ }
                <nav className="navbar navbar-toggleable-md">
                    <a className="navbar-brand" href="#" id="uma-title">{t('websiteTitle')}</a>
                    <ul className="navbar-nav mr-auto">
                    </ul>
                    {this.state.isLoggedIn && (<IconButton iconClassName="material-icons" onClick={() => self.toggleValue('isDrawerDisplayed')}>&#xE8B8;</IconButton>)}
                </nav>
                { /* Display component */}
                <section id="body">
                    {this.props.children}
                </section>
            </section>
            { this.state.isLoggedIn && (<div>
                    <iframe ref={(elt) => { self._sessionFrame = elt; self.startCheckSession(); }} id="session-frame" src="http://localhost:60000/check_session" style={{display: "none"}} /> 
                </div>
            )}
        </div>);
    }

    componentDidMount() {
        var self = this;
        var isLoggedIn = !SessionService.isExpired();
        self.setState({
            isLoggedIn: isLoggedIn
        });
        self._appDispatcher = AppDispatcher.register(function (payload) {
            switch (payload.actionName) {
                case Constants.events.USER_LOGGED_IN:
                    self.setState({
                        isLoggedIn: true
                    });
                    self.refresh();
                    break;
            }
        });
        if (isLoggedIn) {
            self.refresh();
        }
    }

    componentWillUnmount() {
        AppDispatcher.unregister(this._appDispatcher);
    }
}

export default translate('common', { wait: process && !process.release })(withRouter(Layout));