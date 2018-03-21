﻿import React, { Component } from "react";
import { translate } from 'react-i18next';
import { KibanaTab, OpenIdTab, UmaTab } from './logsTabs';
import { withRouter } from 'react-router-dom';

class Logs extends Component {
    constructor(props) {
        super(props);
        this.navigate = this.navigate.bind(this);
        this.state = {
            tabName: null
        };
    }

    navigate(e, tabName) {
        e.preventDefault();
        this.setState({
            tabName: tabName
        });
        this.props.history.push('/logs/' + tabName);
    }

    render() {
        var self = this;
        const { t } = self.props;
        return (<div>
            <ul className="nav nav-tabs">
                <li className="nav-item">
                    <a href="#" className="nav-link" onClick={(e) => self.navigate(e, "openid")}>{t('openid')}</a>
                </li>
                <li className="nav-item">
                    <a href="#" className="nav-link" onClick={(e) => self.navigate(e, "uma")}>{t('uma')}</a>
                </li>
                <li className="nav-item">
                    <a href="#" className="nav-link" onClick={(e) => self.navigate(e, "kibana")}>{t('kibana')}</a>
                </li>
            </ul>
            <div className="tab-content">
                {self.state.tabName === 'openid' && (<OpenIdTab />) }
                {self.state.tabName === 'uma' && (<UmaTab />) }
                {self.state.tabName === 'kibana' && (<KibanaTab />) }
            </div>
        </div>);
    }

    componentDidMount() {
        var self = this;
        var action = self.props.match.params.action;
        if (!action || (action !== 'openid' && action !== 'uma' && action !== 'kibana')) {
            action = 'openid';
        }

        self.setState({
            tabName: action
        });
    }
}

export default translate('common', { wait: process && !process.release })(withRouter(Logs));